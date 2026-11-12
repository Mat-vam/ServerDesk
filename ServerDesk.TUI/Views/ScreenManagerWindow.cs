using ServerDesk.TUI.Styling;
using System.Collections.ObjectModel;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using ServerDesk.Core.Services;
using ServerDesk.TUI.Localization;
using ServerDesk.TUI.Components;

namespace ServerDesk.TUI.Views;

public sealed class ScreenManagerWindow : Window
{
    private readonly IApplication _app;
    private readonly ScreenConsoleService _consoleService;
    private readonly ScreenConnectionService _connectionService;
    private readonly Device _device;

    private readonly TextField _keyPathField;
    private readonly TextField _passphraseField;
    private readonly TextField _fingerprintField;

    private readonly ListView _sessionList;
    private readonly Label _status;
    private readonly Button _connectButton;
    private readonly Button _refreshButton;


    private readonly TextField _sessionNameField;
    private readonly Button _createButton;

    private List<ScreenSession> _sessions = new();
    private readonly Label _selectedSessionLabel;

    private readonly Button _previewButton;
    private readonly Button _terminateButton;

    private readonly FrameView _connectionPanel;
    private readonly FrameView _sessionsArea;
    private readonly Button _settingsButton;

    private bool _rememberPassphrase;

    private readonly Button _rememberButton;
    private readonly Button _forgetPassphraseButton;

    private bool _isBusy;
    private bool _isClosing;
    public ScreenManagerWindow(
        IApplication app,
        ScreenManagerService screenService,
        SshSettingsService sshSettingsService,
        Device device,
        ScreenConsoleService consoleService)
    {
        _app = app;
        _consoleService = consoleService;
        _connectionService = screenService.CreateConnectionService(device, sshSettingsService);
        _device = device;

        var savedSettings = _connectionService.GetInitialSettings();

        _rememberPassphrase = savedSettings.RememberPassphrase;

        Title = UiText.Format("ScreenManagerTitle", device.Name);

        _settingsButton = new Button
        {
            Text = UiText.Get("SessionList"),
            X = 1,
            Y = 0,
            ShadowStyle = null
        };

        var backButton = new Button
        {
            Text = UiText.Get("Back"),
            X = Pos.AnchorEnd(12),
            Y = 0,
            Width = 12,
            ShadowStyle = null
        };

        var hostLabel = new Label
        {
            Text = $"{device.Name} | {device.Host}:{device.Port}",
            X = 1,
            Y = 1,
            Width = Dim.Fill(1)
        };

        // Panele współdzielą miejsce i są wyświetlane naprzemiennie.
        _connectionPanel = new FrameView
        {
            Title = UiText.Get("SshSettings"),
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ShadowStyle = null
        };

        _sessionsArea = new FrameView
        {
            Title = UiText.Get("SessionManagement"),
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ShadowStyle = null,
            Visible = false
        };

        var keyPathLabel = new Label
        {
            Text = UiText.Get("PrivateKeyLabel"),
            X = 1,
            Y = 0
        };

        _keyPathField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = savedSettings.PrivateKeyPath
        };

        var passphraseLabel = new Label
        {
            Text = UiText.Get("PassphraseLabel"),
            X = 1,
            Y = 2
        };

        _passphraseField = new TextField
        {
            X = 1,
            Y = 3,
            Width = Dim.Fill(2),
            Secret = true
        };

        _rememberButton = new Button
        {
            Text = _rememberPassphrase
                ? UiText.Get("RememberPassphraseOn")
                : UiText.Get("RememberPassphraseOff"),
            X = 1,
            Y = 4,
            ShadowStyle = null
        };

        _forgetPassphraseButton = new Button
        {
            Text = UiText.Get("ForgetPassphrase"),
            X = Pos.Right(_rememberButton) + 2,
            Y = 4,
            ShadowStyle = null
        };

        var fingerprintLabel = new Label
        {
            Text = UiText.Get("HostFingerprintLabel"),
            X = 1,
            Y = 5
        };

        _fingerprintField = new TextField
        {
            X = 1,
            Y = 6,
            Width = Dim.Fill(2),
            Text = savedSettings.HostFingerprint
        };

        _connectButton = new Button
        {
            Text = UiText.Get("Connect"),
            X = 1,
            Y = 8,
            ShadowStyle = null
        };

        _connectionPanel.Add(
            keyPathLabel,
            _keyPathField,
            passphraseLabel,
            _passphraseField,
            _rememberButton,
            _forgetPassphraseButton,
            fingerprintLabel,
            _fingerprintField,
            _connectButton);

        var sessionNameLabel = new Label
        {
            Text = UiText.Get("NewSessionNameLabel"),
            X = 1,
            Y = 0
        };

        _sessionNameField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = "serverdesk"
        };

        _refreshButton = new Button
        {
            Text = UiText.Get("Refresh"),
            X = 1,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        _createButton = new Button
        {
            Text = UiText.Get("CreateSessionAction"),
            X = Pos.Right(_refreshButton) + 1,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        _terminateButton = new Button
        {
            Text = UiText.Get("TerminateSessionAction"),
            X = Pos.Right(_createButton) + 1,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        _previewButton = new Button
        {
            Text = UiText.Get("OpenConsoleAction"),
            X = Pos.Right(_terminateButton) + 1,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        var sessionsPanel = new FrameView
        {
            Title = UiText.Get("GnuScreenSessions"),
            X = 0,
            Y = 5,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ShadowStyle = null
        };

        _sessionList = new ListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        sessionsPanel.Add(_sessionList);

        _sessionsArea.Add(
            sessionNameLabel,
            _sessionNameField,
            _refreshButton,
            _createButton,
            _terminateButton,
            _previewButton,
            sessionsPanel);

        _selectedSessionLabel = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(1),
            Text = UiText.Get("NoSessionSelected")
        };

        _status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = UiText.Get("ConnectHint")
        };

        Add(
            _settingsButton,
            backButton,
            hostLabel,
            _connectionPanel,
            _sessionsArea,
            _selectedSessionLabel,
            _status);

        KeyboardNavigation.Configure(
            this,
            _settingsButton,
            backButton,
            _keyPathField,
            _passphraseField,
            _rememberButton,
            _forgetPassphraseButton,
            _fingerprintField,
            _connectButton,
            _sessionNameField,
            _refreshButton,
            _createButton,
            _terminateButton,
            _previewButton,
            _sessionList);

        _sessionList.SetSource(
            new ObservableCollection<string>([UiText.Get("SessionsNotLoaded")]));

        _settingsButton.Accepted += (_, _) =>
        {
            if (_isBusy || _isClosing)
                return;

            if (_connectionPanel.Visible)
            {
                if (!EnsureConnectionReady())
                    return;

                ShowConnectionSettings(false);
            }
            else
            {
                ShowConnectionSettings(true);
            }
        };

        backButton.Accepted += (_, _) =>
        {
            if (_isBusy)
            {
                _status.Text = UiText.Get("WaitForOperation");
                return;
            }

            _app.RequestStop(this);
        };

        _connectButton.Accepted += (_, _) =>
        {
            _ = RefreshSessionsAsync(connect: true);
        };

        _refreshButton.Accepted += (_, _) =>
        {
            _ = RefreshSessionsAsync();
        };

        _createButton.Accepted += (_, _) =>
        {
            _ = CreateSessionAsync();
        };

        _previewButton.Accepted += (_, _) =>
        {
            OpenSelectedConsole();
        };

        _terminateButton.Accepted += (_, _) =>
        {
            _ = TerminateSelectedSessionAsync();
        };

        _rememberButton.Accepted += (_, _) =>
        {
            if (_isBusy)
                return;

            _rememberPassphrase = !_rememberPassphrase;

            _rememberButton.Text = _rememberPassphrase
                ? UiText.Get("RememberPassphraseOn")
                : UiText.Get("RememberPassphraseOff");

            _status.Text = UiText.Get("ConnectToSave");
        };

        _forgetPassphraseButton.Accepted += (_, _) =>
        {
            _ = ForgetPassphraseAsync();
        };

        _sessionList.ValueChanged += (_, _) =>
        {
            UpdateSelectedSession();
        };

        if (_rememberPassphrase)
        {
            try
            {
                string? savedPassphrase =
                    _connectionService.LoadSavedPassphrase();

                if (savedPassphrase is not null)
                {
                    _passphraseField.Text = savedPassphrase;
                    _status.Text = UiText.Get("SavedPassphraseLoaded");
                }
                else
                {
                    _status.Text = UiText.Get("SavedPassphraseMissing");
                }
            }
            catch (Exception ex)
            {
                _status.Text = UiText.Format("PassphraseLoadFailed", ErrorText.Get(ex));
            }
        }

        Disposing += (_, _) =>
        {
            _isClosing = true;
        };

        _keyPathField.SetFocus();

        AppTheme.Apply(this);

        _terminateButton.SetScheme(AppTheme.CreateWarning());
        _selectedSessionLabel.SetScheme(AppTheme.CreateAccent());
    }
    private ScreenConnectionData ReadConnectionData()
    {
        return new ScreenConnectionData(
            _keyPathField.Text,
            _passphraseField.Text,
            _fingerprintField.Text,
            _rememberPassphrase);
    }

    private bool EnsureConnectionReady()
    {
        if (_connectionService.IsReady(ReadConnectionData()))
            return true;

        _refreshButton.Enabled = false;
        _createButton.Enabled = false;
        _previewButton.Enabled = false;
        _terminateButton.Enabled = false;

        UpdateSessionList(Array.Empty<ScreenSession>());

        _status.Text = UiText.Get("ConnectionNotVerified");

        return false;
    }

    private void ShowConnectionSettings(bool showSettings)
    {
        _connectionPanel.Visible = showSettings;
        _sessionsArea.Visible = !showSettings;

        _settingsButton.Text = showSettings
            ? UiText.Get("SessionList")
            : UiText.Get("SshSettings");

        if (showSettings)
            _keyPathField.SetFocus();
        else
            _sessionList.SetFocus();
    }

    private async Task ForgetPassphraseAsync()
    {
        if (_isBusy || _isClosing)
            return;

        _isBusy = true;
        _forgetPassphraseButton.Enabled = false;
        _rememberButton.Enabled = false;

        try
        {
            await _connectionService.ForgetPassphraseAsync();

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _rememberPassphrase = false;
                _rememberButton.Text = UiText.Get("RememberPassphraseOff");
                _passphraseField.Text = "";

                _refreshButton.Enabled = false;
                _createButton.Enabled = false;
                _previewButton.Enabled = false;
                _terminateButton.Enabled = false;

                UpdateSessionList(Array.Empty<ScreenSession>());

                _status.Text = UiText.Get("PassphraseForgotten");
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text = UiText.Format("PassphraseDeleteFailed", ErrorText.Get(ex));
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                _isBusy = false;

                if (_isClosing)
                    return;

                _forgetPassphraseButton.Enabled = true;
                _rememberButton.Enabled = true;
            });
        }
    }

    private void UpdateSelectedSession()
    {
        if (_sessionList.Value is not int index ||
            index < 0 ||
            index >= _sessions.Count)
        {
            _selectedSessionLabel.Text = UiText.Get("NoSessionSelected");
            return;
        }

        var session = _sessions[index];

        _selectedSessionLabel.Text = UiText.Format(
            "SelectedScreenSession",
            session.Id,
            ScreenStateText.Get(session.State));
    }

    private void UpdateSessionList(
        IReadOnlyList<ScreenSession> sessions)
    {
        string? previousId = null;

        if (_sessionList.Value is int previousIndex &&
            previousIndex >= 0 &&
            previousIndex < _sessions.Count)
        {
            previousId = _sessions[previousIndex].Id;
        }

        _sessions = sessions.ToList();

        var names = _sessions.Count == 0
            ? new ObservableCollection<string>(
                [UiText.Get("NoActiveSessions")])
            : new ObservableCollection<string>(
                _sessions.Select(s =>
                    $"{s.Id} | {ScreenStateText.Get(s.State)}"));

        _sessionList.SetSource(names);

        if (_sessions.Count > 0)
        {
            int index = _sessions.FindIndex(
                s => s.Id == previousId);

            if (index < 0)
                index = 0;

            _sessionList.Value = index;
        }

        UpdateSelectedSession();
    }


    private async Task RefreshSessionsAsync(bool connect = false)
    {
        if (_isBusy || _isClosing)
            return;

        if (!connect && !EnsureConnectionReady())
            return;

        ScreenConnectionData connection = ReadConnectionData();

        if (connect)
            UpdateSessionList(Array.Empty<ScreenSession>());

        _isBusy = true;

        _connectButton.Enabled = false;
        _refreshButton.Enabled = false;
        _createButton.Enabled = false;
        _previewButton.Enabled = false;
        _terminateButton.Enabled = false;

        _keyPathField.Enabled = false;
        _passphraseField.Enabled = false;
        _fingerprintField.Enabled = false;
        _rememberButton.Enabled = false;
        _forgetPassphraseButton.Enabled = false;

        _status.Text = connect
            ? UiText.Get("CheckingSshAccess")
            : UiText.Get("RefreshingSessions");

        try
        {
            var sessions = await Task.Run(() =>
                connect
                    ? _connectionService.ConnectAsync(connection)
                    : _connectionService.RefreshAsync(connection));

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                UpdateSessionList(sessions);

                if (connect)
                    ShowConnectionSettings(false);

                _status.Text = connect
                    ? UiText.Format("SshAccessVerified", sessions.Count)
                    : UiText.Format("SessionsRefreshed", sessions.Count);
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _status.Text = connect
                    ? UiText.Format("ConnectionFailed", ErrorText.Get(ex))
                    : UiText.Format("SessionsRefreshFailed", ErrorText.Get(ex));
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                _isBusy = false;

                if (_isClosing)
                    return;

                bool verified =
                    _connectionService.IsReady(ReadConnectionData());

                _connectButton.Enabled = true;
                _refreshButton.Enabled = verified;
                _createButton.Enabled = verified;
                _previewButton.Enabled = verified;
                _terminateButton.Enabled = verified;

                _keyPathField.Enabled = true;
                _passphraseField.Enabled = true;
                _fingerprintField.Enabled = true;
                _rememberButton.Enabled = true;
                _forgetPassphraseButton.Enabled = true;
            });
        }
    }

    private void OpenSelectedConsole()
    {
        if (_isBusy || _isClosing)
            return;

        if (!EnsureConnectionReady())
            return;

        if (_sessionList.Value is not int index ||
            index < 0 ||
            index >= _sessions.Count)
        {
            _status.Text = UiText.Get("SelectSessionFirst");
            return;
        }

        var connection =
            _connectionService.GetVerifiedConnection(ReadConnectionData());

        string sessionId = _sessions[index].Id;

        _isBusy = true;

        try
        {
            using var consoleWindow = new ScreenConsoleWindow(
                _app,
                _consoleService,
                _device,
                sessionId,
                connection.KeyPath,
                connection.Passphrase,
                connection.Fingerprint);

            _app.Run(consoleWindow);

            _status.Text = UiText.Format("ReturnedFromConsole", sessionId);
        }
        catch (Exception ex)
        {
            _status.Text = UiText.Format("ConsoleOpenFailed", ErrorText.Get(ex));
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task TerminateSelectedSessionAsync()
    {
        if (_isBusy || _isClosing)
            return;

        if (!EnsureConnectionReady())
            return;

        if (_sessionList.Value is not int index ||
            index < 0 ||
            index >= _sessions.Count)
        {
            _status.Text = UiText.Get("SelectSessionFirst");
            return;
        }

        // Zapamiętanie sesji i danych połączenia przed wyświetleniem potwierdzenia.
        string sessionId = _sessions[index].Id;
        ScreenConnectionData connection = ReadConnectionData();

        _isBusy = true;

        try
        {
            int? answer = MessageBox.Query(
                _app,
                UiText.Get("TerminateSessionTitle"),
                string.Join(
                    Environment.NewLine,
                    UiText.Format("DeviceLabelFormat", _device.Name),
                    UiText.Format("HostLabelFormat", _device.Host, _device.Port),
                    UiText.Format("SessionLabel", sessionId),
                    "",
                    UiText.Get("TerminateSessionWarning"),
                    "",
                    UiText.Get("ContinuePrompt")),
                UiText.Get("Cancel"),
                UiText.Get("TerminateSessionConfirm"));

            if (answer != 1)
                return;

            _connectButton.Enabled = false;
            _refreshButton.Enabled = false;
            _createButton.Enabled = false;
            _previewButton.Enabled = false;
            _terminateButton.Enabled = false;

            _keyPathField.Enabled = false;
            _passphraseField.Enabled = false;
            _fingerprintField.Enabled = false;
            _rememberButton.Enabled = false;
            _forgetPassphraseButton.Enabled = false;

            _status.Text = UiText.Format("TerminatingSession", sessionId);

            var result = await Task.Run(
                () => _connectionService.TerminateSessionAsync(
                    connection,
                    sessionId));

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (result.Sessions is not null)
                    UpdateSessionList(result.Sessions);
                else
                    UpdateSessionList(Array.Empty<ScreenSession>());

                if (result.RefreshError is not null)
                {
                    _status.Text = UiText.Format(
                        "TerminationRefreshFailed",
                        ErrorText.Get(result.RefreshError));
                }
                else
                {
                    _status.Text = result.SessionStillListed
                        ? UiText.Get("TerminationPending")
                        : UiText.Format("SessionTerminated", sessionId);
                }
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _status.Text = UiText.Format(
                    "SessionOperationFailed",
                    ErrorText.Get(ex));
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                _isBusy = false;

                if (_isClosing)
                    return;

                bool verified =
                    _connectionService.IsReady(ReadConnectionData());

                _connectButton.Enabled = true;
                _refreshButton.Enabled = verified;
                _createButton.Enabled = verified;
                _previewButton.Enabled = verified;
                _terminateButton.Enabled = verified;

                _keyPathField.Enabled = true;
                _passphraseField.Enabled = true;
                _fingerprintField.Enabled = true;
                _rememberButton.Enabled = true;
                _forgetPassphraseButton.Enabled = true;
            });
        }
    }

    private async Task CreateSessionAsync()
    {
        if (_isBusy || _isClosing)
            return;

        if (!EnsureConnectionReady())
            return;

        ScreenConnectionData connection = ReadConnectionData();
        string sessionName = _sessionNameField.Text;

        _isBusy = true;
        _refreshButton.Enabled = false;
        _createButton.Enabled = false;
        _status.Text = UiText.Get("CreatingSession");

        try
        {
            var result = await Task.Run(
                () => _connectionService.CreateSessionAsync(
                    connection,
                    sessionName));

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (result.Sessions is not null)
                    UpdateSessionList(result.Sessions);
                else
                    UpdateSessionList(Array.Empty<ScreenSession>());

                _status.Text = result.RefreshError is null
                    ? UiText.Format("SessionCreated", sessionName)
                    : UiText.Format("CreationRefreshFailed", ErrorText.Get(result.RefreshError));
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text = UiText.Format("SessionCreationFailed", ErrorText.Get(ex));
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _refreshButton.Enabled = true;
                _createButton.Enabled = true;
                _isBusy = false;
            });
        }
    }
}
