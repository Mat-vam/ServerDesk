using ServerDesk.Core.Models;
using ServerDesk.Core.Services;
using ServerDesk.TUI.Localization;
using ServerDesk.TUI.Styling;
using Terminal.Gui.App;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using ServerDesk.TUI.Components;

namespace ServerDesk.TUI.Views;

public sealed class ScreenConsoleWindow : Window
{
    private readonly IApplication _app;
    private readonly ScreenConsoleService _consoleService;
    private readonly Device _device;
    private readonly string _sessionId;

    private readonly string _keyPath;
    private readonly string _passphrase;
    private readonly string _fingerprint;

    private readonly TextView _consoleOutput;
    private readonly TextField _commandField;
    private readonly Button _sendButton;
    private readonly Button _refreshButton;
    private readonly Button _backButton;
    private readonly Label _status;

    private bool _isBusy;
    private bool _isClosing;

    public ScreenConsoleWindow(IApplication app, ScreenConsoleService consoleService, Device device, string sessionId, string keyPath, string passphrase, string fingerprint)
    {
        _app = app;
        _consoleService = consoleService;
        _device = device;
        _sessionId = sessionId;
        _keyPath = keyPath;
        _passphrase = passphrase;
        _fingerprint = fingerprint;

        Title = UiText.Format(
            "ConsoleWindowTitle",
            device.Name,
            sessionId);

        var consolePanel = new FrameView
        {
            Title = UiText.Get("ConsolePreview"),
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(6),
            ShadowStyle = null
        };

        _consoleOutput = new TextView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            WordWrap = false,
            Text = UiText.Get("LoadingConsole")
        };

        consolePanel.Add(_consoleOutput);

        var commandLabel = new Label
        {
            Text = UiText.Get("CommandLabel"),
            X = 1,
            Y = Pos.AnchorEnd(5)
        };

        _commandField = new TextField
        {
            X = 1,
            Y = Pos.AnchorEnd(4),
            Width = Dim.Fill(1)
        };

        _sendButton = new Button
        {
            Text = UiText.Get("Send"),
            X = 1,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _refreshButton = new Button
        {
            Text = UiText.Get("Refresh"),
            X = Pos.Right(_sendButton) + 2,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _backButton = new Button
        {
            Text = UiText.Get("Back"),
            X = Pos.Right(_refreshButton) + 2,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _status = new Label
        {
            Text = UiText.Format("SessionLabel", sessionId),
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1)
        };

        Add(
            consolePanel,
            commandLabel,
            _commandField,
            _sendButton,
            _refreshButton,
            _backButton,
            _status);

        KeyboardNavigation.Configure(
            this,
            _consoleOutput,
            _commandField,
            _sendButton,
            _refreshButton,
            _backButton);

        _sendButton.Accepted += (_, _) =>
        {
            _ = ExecuteOperationAsync(sendCommand: true);
        };

        _commandField.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                // Zatrzymujemy dalsza obsluge tego Entera.
                key.Handled = true;

                _ = ExecuteOperationAsync(sendCommand: true);
            }
        };

        _refreshButton.Accepted += (_, _) =>
        {
            _ = ExecuteOperationAsync(sendCommand: false);
        };

        _backButton.Accepted += (_, _) =>
        {
            if (!_isBusy)
                _app.RequestStop(this);
        };

        Disposing += (_, _) =>
        {
            _isClosing = true;
        };

        IsRunningChanged += (_, e) =>
        {
            if (e.Value == true)
                _ = ExecuteOperationAsync(sendCommand: false);
        };

        AppTheme.Apply(this);

        _commandField.SetFocus();
    }

    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        _commandField.Enabled = !busy;
        _sendButton.Enabled = !busy;
        _refreshButton.Enabled = !busy;
        _backButton.Enabled = !busy;
    }

    private async Task ExecuteOperationAsync(bool sendCommand)
    {
        if (_isBusy || _isClosing)
            return;

        string text = _commandField.Text;

        SetBusy(true);

        _status.Text = sendCommand
            ? UiText.Get("SendingCommand")
            : UiText.Get("LoadingConsole");

        try
        {
            // Widok wybiera operacje na podstawie akcji uzytkownika.
            // Jej przebieg i walidacja znajduja sie w serwisie.
            var result = await Task.Run(() =>
                sendCommand
                    ? _consoleService.SendAndRefreshAsync(
                        _device,
                        _keyPath,
                        _passphrase,
                        _fingerprint,
                        _sessionId,
                        text)
                    : _consoleService.RefreshAsync(
                        _device,
                        _keyPath,
                        _passphrase,
                        _fingerprint,
                        _sessionId));

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (result.CommandSent)
                    _commandField.Text = "";

                if (result.RefreshError is not null)
                {
                    _status.Text = UiText.Format(
                        "CommandSentRefreshFailed",
                        ErrorText.Get(result.RefreshError));

                    return;
                }

                // Formatowanie tekstu nalezy do prezentacji.
                string displayOutput = result.Output?.TrimEnd() ?? "";

                _consoleOutput.Text =
                    string.IsNullOrWhiteSpace(displayOutput)
                        ? UiText.Get("ConsoleEmpty")
                        : displayOutput;

                _consoleOutput.MoveEnd();

                _status.Text = result.CommandSent
                    ? UiText.Format("CommandSentAndRefreshed", DateTime.Now)
                    : UiText.Format("RefreshedAt", DateTime.Now);
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text = UiText.Format(
                        "ErrorDetails",
                        ErrorText.Get(ex));
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                SetBusy(false);

                if (sendCommand)
                    _commandField.SetFocus();
            });
        }
    }
}
