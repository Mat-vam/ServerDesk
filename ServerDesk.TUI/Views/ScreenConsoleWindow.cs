using ServerDesk.TUI.Styling;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui.Input;

namespace ServerDesk.TUI.Views;

public sealed class ScreenConsoleWindow : Window
{
    private readonly IApplication _app;
    private readonly IScreenService _screenService;
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

    public ScreenConsoleWindow(
        IApplication app,
        IScreenService screenService,
        Device device,
        string sessionId,
        string keyPath,
        string passphrase,
        string fingerprint)
    {
        _app = app;
        _screenService = screenService;
        _device = device;
        _sessionId = sessionId;
        _keyPath = keyPath;
        _passphrase = passphrase;
        _fingerprint = fingerprint;

        Title = $"ServerDesk | {device.Name} | {sessionId} | okno 0";

        var consolePanel = new FrameView
        {
            Title = "Podglad konsoli",
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
            Text = "Pobieranie konsoli..."
        };

        consolePanel.Add(_consoleOutput);

        var commandLabel = new Label
        {
            Text = "Polecenie:",
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
            Text = "Wyslij",
            X = 1,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _refreshButton = new Button
        {
            Text = "Odswiez",
            X = Pos.Right(_sendButton) + 2,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _backButton = new Button
        {
            Text = "Wroc",
            X = Pos.Right(_refreshButton) + 2,
            Y = Pos.AnchorEnd(3),
            ShadowStyle = null
        };

        _status = new Label
        {
            Text = $"Sesja: {sessionId}",
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

        if (sendCommand && string.IsNullOrWhiteSpace(text))
        {
            _status.Text = "Wpisz polecenie.";
            return;
        }

        SetBusy(true);

        _status.Text = sendCommand
            ? "Wysylanie polecenia..."
            : "Pobieranie konsoli...";

        bool commandSent = false;

        try
        {
            string output = await Task.Run(async () =>
            {
                if (sendCommand)
                {
                    await _screenService.SendCommandAsync(
                        _device,
                        _keyPath,
                        _passphrase,
                        _fingerprint,
                        _sessionId,
                        text);

                    commandSent = true;

                    // Krotka chwila na pojawienie sie odpowiedzi
                    // programu w konsoli. Nie blokuje interfejsu.
                    await Task.Delay(250);
                }

                // Pobieramy podglad zarowno po wyslaniu,
                // jak i przy zwyklym odswiezaniu.
                return await _screenService.GetConsoleOutputAsync(
                    _device,
                    _keyPath,
                    _passphrase,
                    _fingerprint,
                    _sessionId);
            });

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (commandSent)
                    _commandField.Text = "";

                string displayOutput = output.TrimEnd();

                _consoleOutput.Text =
                    string.IsNullOrWhiteSpace(displayOutput)
                        ? "(Konsola jest pusta.)"
                        : displayOutput;

                _consoleOutput.MoveEnd();

                _status.Text = commandSent
                    ? $"Wyslano i odswiezono: {DateTime.Now:HH:mm:ss}"
                    : $"Odswiezono: {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (commandSent)
                {
                    // Polecenie juz wyslano. Blad dotyczy odczytu,
                    // wiec nie sugerujemy ponownego wysylania.
                    _commandField.Text = "";

                    _status.Text =
                        $"Polecenie wyslano, ale podglad nie zostal " +
                        $"odswiezony: {ex.Message}";
                }
                else
                {
                    _status.Text = $"Blad: {ex.Message}";
                }
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