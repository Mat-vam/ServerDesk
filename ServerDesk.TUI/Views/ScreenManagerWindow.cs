using System.Collections.ObjectModel;
using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using ServerDesk.Core.Services;
using ServerDesk.Infrastructure.Security;

namespace ServerDesk.TUI.Views;

public sealed class ScreenManagerWindow : Window
{
    private readonly IApplication _app;
    private readonly IScreenService _screenService;
    private readonly DeviceService _deviceService;
    private readonly Device _device;

    private readonly TextField _keyPathField;
    private readonly TextField _passphraseField;
    private readonly TextField _fingerprintField;

    private readonly ListView _sessionList;
    private readonly Label _status;
    private readonly Button _refreshButton;

    private readonly TextField _sessionNameField;
    private readonly Button _createButton;

    private List<ScreenSession> _sessions = new();
    private readonly Label _selectedSessionLabel;

    private readonly TextField _commandField;
    private readonly Button _sendCommandButton;

    private readonly WindowsCredentialStore _credentialStore = new();

    private bool _rememberPassphrase;

    private readonly Button _rememberButton;
    private readonly Button _forgetPassphraseButton;

    private bool _isBusy;
    private bool _isClosing;
    public ScreenManagerWindow(IApplication app, IScreenService screenService, DeviceService deviceService, Device device)
    {
        _app = app;
        _screenService = screenService;
        _device = device;
        _deviceService = deviceService;
        _rememberPassphrase = _device.SshSettings?.RememberPassphrase ?? false;

        Title = $"ServerDesk | Screen Manager | {device.Name}";

        Add(new Label
        {
            Text = $"Host: {device.Host}:{device.Port}",
            X = 1,
            Y = 1
        });

        Add(new Label
        {
            Text = "Sciezka do klucza prywatnego:",
            X = 1,
            Y = 3
        });


        var savedSettings = _device.SshSettings ?? new SshConnectionSettings();

        string defaultKeyPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".ssh", "serverdesk_ed25519");

        _keyPathField = new TextField
        {
            X = 1,
            Y = 4,
            Width = Dim.Fill(2),
            Text = string.IsNullOrWhiteSpace(
                savedSettings.PrivateKeyPath)
                ? defaultKeyPath
                : savedSettings.PrivateKeyPath
        };


        Add(new Label
        {
            Text = "Passphrase klucza:",
            X = 1,
            Y = 6
        });

        _passphraseField = new TextField
        {
            X = 1,
            Y = 7,
            Width = Dim.Fill(2),
            Secret = true
        };

        Add(new Label
        {
            Text = "Zaufany odcisk hosta SHA256:",
            X = 1,
            Y = 9
        });


        _rememberButton = new Button
        {
            Text = _rememberPassphrase
                ? "Zapamietywanie: TAK"
                : "Zapamietywanie: NIE",
            X = 1,
            Y = 8,
            ShadowStyle = null
        };

        _forgetPassphraseButton = new Button
        {
            Text = "Zapomnij haslo",
            X = Pos.Right(_rememberButton) + 2,
            Y = 8,
            ShadowStyle = null
        };

        _rememberButton.Accepted += (_, _) =>
        {
            if (_isBusy)
                return;

            _rememberPassphrase = !_rememberPassphrase;

            _rememberButton.Text = _rememberPassphrase
                ? "Zapamietywanie: TAK"
                : "Zapamietywanie: NIE";

            _status.Text = _rememberPassphrase
                ? "Zapamietywanie wlaczone. Polacz, aby zapisac."
                : "Zapamietywanie wylaczone. Polacz, aby zapisac zmiane.";
        };

        _forgetPassphraseButton.Accepted += (_, _) =>
        {
            _ = ForgetPassphraseAsync();
        };


        _fingerprintField = new TextField
        {
            X = 1,
            Y = 10,
            Width = Dim.Fill(2),
            Text = savedSettings.HostFingerprint
        };

        var sessionNameLabel = new Label
        {
            Text = "Nazwa nowej sesji:",
            X = 1,
            Y = 12
        };

        _sessionNameField = new TextField
        {
            X = 1,
            Y = 13,
            Width = Dim.Fill(2),
            Text = "serverdesk_test"
        };


        _refreshButton = new Button
        {
            Text = "Polacz i odswiez",
            X = 1,
            Y = 15,
            ShadowStyle = null
        };

        _createButton = new Button
        {
            Text = "Utworz sesje",
            X = Pos.Right(_refreshButton) + 2,
            Y = 15,
            ShadowStyle = null
        };

        var commandLabel = new Label
        {
            Text = "Polecenie do wybranej sesji:",
            X = 1,
            Y = 17
        };

        _commandField = new TextField
        {
            X = 1,
            Y = 18,
            Width = Dim.Fill(2)
        };

        _sendCommandButton = new Button
        {
            Text = "Wyslij polecenie",
            X = 1,
            Y = 19,
            ShadowStyle = null
        };


        var sessionsPanel = new FrameView
        {
            Title = "Sesje GNU Screen",
            X = 0,
            Y = 21,
            Width = Dim.Fill(),
            Height = Dim.Fill(3),
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

        _status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = "Gotowe do polaczenia."
        };

        _selectedSessionLabel = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(1),
            Text = "Nie wybrano sesji."
        };


        if (_rememberPassphrase)
        {
            try
            {
                string? savedPassphrase =
                    _credentialStore.Load(_device.Id);

                if (savedPassphrase is not null)
                {
                    _passphraseField.Text = savedPassphrase;
                    _status.Text =
                        "Passphrase wczytane z magazynu Windows.";
                }
                else
                {
                    _status.Text =
                        "Brak zapisanego passphrase. Wpisz je ponownie.";
                }
            }
            catch (Exception ex)
            {
                _status.Text =
                    $"Nie mozna odczytac hasla: {ex.Message}";
            }
        }


        Add(
            _keyPathField,
            _passphraseField,
            _fingerprintField,
            sessionNameLabel,
            _sessionNameField,
            _refreshButton,
            _createButton,
            commandLabel,
            _commandField,
            _sendCommandButton,
            sessionsPanel,
            _selectedSessionLabel,
            _status,
            _rememberButton,
            _forgetPassphraseButton
        );


        _sessionList.SetSource(
            new ObservableCollection<string>(
                ["Nie pobrano jeszcze sesji."]));

        _refreshButton.Accepted += (_, _) =>
        {
            _ = RefreshSessionsAsync();
        };

        _createButton.Accepted += (_, _) =>
        {
            _ = CreateSessionAsync();
        };

        _sendCommandButton.Accepted += (_, _) =>
        {
            _ = SendSelectedCommandAsync();
        };

        _keyPathField.SetFocus();

        Disposing += (_, _) =>
        {
            _isClosing = true;
        };

        _sessionList.ValueChanged += (_, _) =>
        {
            UpdateSelectedSession();
        };
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
            _credentialStore.Delete(_device.Id);

            var previous = _device.SshSettings
                ?? new SshConnectionSettings();

            var settings = new SshConnectionSettings
            {
                PrivateKeyPath = previous.PrivateKeyPath,
                HostFingerprint = previous.HostFingerprint,
                RememberPassphrase = false,
                AutoConnect = previous.AutoConnect
            };

            bool saved = await _deviceService.UpdateSshSettingsAsync(
                _device.Id,
                settings);

            if (!saved)
                throw new InvalidOperationException(
                    "Nie znaleziono urzadzenia.");

            _device.SshSettings = settings;

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _rememberPassphrase = false;
                _rememberButton.Text = "Zapamietywanie: NIE";
                _passphraseField.Text = "";
                _status.Text = "Zapomniano zapisane passphrase.";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text =
                        $"Blad usuwania hasla: {ex.Message}";
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
            _selectedSessionLabel.Text = "Nie wybrano sesji.";
            return;
        }

        var session = _sessions[index];

        _selectedSessionLabel.Text =
            $"Wybrano: {session.Id} | {session.State}";
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
                ["Brak aktywnych sesji screen."])
            : new ObservableCollection<string>(
                _sessions.Select(s => $"{s.Id} | {s.State}"));

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


    private async Task RefreshSessionsAsync()
    {
        if (_isBusy)
            return;

        string keyPath = _keyPathField.Text.Trim();
        string passphrase = _passphraseField.Text;
        string fingerprint = _fingerprintField.Text.Trim();

        if (!File.Exists(keyPath))
        {
            _status.Text = "Nie znaleziono klucza prywatnego.";
            return;
        }

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            _status.Text = "Podaj odcisk SHA256 hosta.";
            return;
        }

        _isBusy = true;
        _refreshButton.Enabled = false;
        _status.Text = "Laczenie z serwerem...";

        try
        {
            // W tej wersji lista jest pobierana za kazdym
            // kliknieciem przez osobne polaczenie SSH.
            var sessions = await Task.Run(
                () => _screenService.GetSessionsAsync(
                    _device,
                    keyPath,
                    passphrase,
                    fingerprint));

            await SaveConnectionSettingsAsync(keyPath, fingerprint, passphrase);

            // UI aktualizujemy na watku Terminal.Gui.
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                var names = sessions.Count == 0
                    ? new ObservableCollection<string>(
                        ["Brak aktywnych sesji screen."])
                    : new ObservableCollection<string>(
                        sessions.Select(s =>
                            $"{s.Id} | {s.State}"));

                UpdateSessionList(sessions);

                _status.Text =
                    $"Polaczono | Liczba sesji: {sessions.Count}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text =
                        $"Blad polaczenia: {ex.Message}";
            });
        }
        finally
        {
            // Nie przechowujemy passphrase w polu tekstowym.
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _refreshButton.Enabled = true;
                _isBusy = false;
            });
        }
    }


    private async Task SendSelectedCommandAsync()
    {
        if (_isBusy || _isClosing)
            return;

        if (_sessionList.Value is not int index ||
            index < 0 ||
            index >= _sessions.Count)
        {
            _status.Text = "Najpierw wybierz sesje.";
            return;
        }

        var session = _sessions[index];

        string keyPath = _keyPathField.Text.Trim();
        string passphrase = _passphraseField.Text;
        string fingerprint = _fingerprintField.Text.Trim();
        string text = _commandField.Text;

        if (!File.Exists(keyPath))
        {
            _status.Text = "Nie znaleziono klucza SSH.";
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            _status.Text = "Wpisz polecenie.";
            return;
        }

        _isBusy = true;
        _sendCommandButton.Enabled = false;
        _refreshButton.Enabled = false;
        _createButton.Enabled = false;

        _status.Text = $"Wysylanie do: {session.Name}";

        try
        {
            await _screenService.SendCommandAsync(
                _device,
                keyPath,
                passphrase,
                fingerprint,
                session.Id,
                text);

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                _commandField.Text = "";
                _status.Text =
                    $"Wyslano do sesji: {session.Id}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text =
                        $"Blad wysylania: {ex.Message}";
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                _isBusy = false;

                if (_isClosing)
                    return;

                _sendCommandButton.Enabled = true;
                _refreshButton.Enabled = true;
                _createButton.Enabled = true;
            });
        }
    }


    private async Task CreateSessionAsync()
    {
        if (_isBusy)
            return;

        string keyPath = _keyPathField.Text.Trim();
        string passphrase = _passphraseField.Text;
        string fingerprint = _fingerprintField.Text.Trim();
        string sessionName = _sessionNameField.Text.Trim();

        if (!File.Exists(keyPath))
        {
            _status.Text = "Nie znaleziono klucza prywatnego.";
            return;
        }

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            _status.Text = "Podaj odcisk hosta SHA256.";
            return;
        }

        _isBusy = true;
        _refreshButton.Enabled = false;
        _createButton.Enabled = false;
        _status.Text = "Tworzenie sesji screen...";

        try
        {
            await _screenService.CreateTestSessionAsync(
                _device,
                keyPath,
                passphrase,
                fingerprint,
                sessionName);

            // Pobieramy aktualna liste po utworzeniu sesji.
            var sessions = await _screenService.GetSessionsAsync(
                _device,
                keyPath,
                passphrase,
                fingerprint);

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                var names = sessions.Count == 0
                    ? new ObservableCollection<string>(
                        ["Brak aktywnych sesji screen."])
                    : new ObservableCollection<string>(
                        sessions.Select(s =>
                            $"{s.Id} | {s.State}"));

                UpdateSessionList(sessions);

                _status.Text =
                    $"Utworzono sesje: {sessionName}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (!_isClosing)
                    _status.Text =
                        $"Blad tworzenia sesji: {ex.Message}";
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


    private async Task SaveConnectionSettingsAsync(
        string keyPath,
        string fingerprint,
        string passphrase)
    {
        var previous = _device.SshSettings
            ?? new SshConnectionSettings();

        // Najpierw operacja na bezpiecznym magazynie.
        if (_rememberPassphrase)
        {
            _credentialStore.Save(
                _device.Id,
                passphrase);
        }
        else
        {
            _credentialStore.Delete(_device.Id);
        }

        // Dopiero potem zapis konfiguracji bez hasla.
        var settings = new SshConnectionSettings
        {
            PrivateKeyPath = keyPath,
            HostFingerprint = fingerprint,
            RememberPassphrase = _rememberPassphrase,
            AutoConnect = previous.AutoConnect
        };

        bool saved = await _deviceService.UpdateSshSettingsAsync(
            _device.Id,
            settings);

        if (!saved)
        {
            throw new InvalidOperationException(
                "Nie znaleziono urzadzenia podczas zapisu SSH.");
        }

        _device.SshSettings = settings;
    }



}
