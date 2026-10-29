using ServerDesk.TUI.Styling;
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
    private readonly Button _connectButton;
    private readonly Button _refreshButton;

    private sealed record ConnectionData(string KeyPath, string Passphrase, string Fingerprint, bool RememberPassphrase);

    private ConnectionData? _verifiedConnection;

    private readonly TextField _sessionNameField;
    private readonly Button _createButton;

    private List<ScreenSession> _sessions = new();
    private readonly Label _selectedSessionLabel;

    private readonly Button _previewButton;
    private readonly Button _terminateButton;

    private readonly FrameView _connectionPanel;
    private readonly FrameView _sessionsArea;
    private readonly Button _settingsButton;

    private readonly WindowsCredentialStore _credentialStore = new();

    private bool _rememberPassphrase;

    private readonly Button _rememberButton;
    private readonly Button _forgetPassphraseButton;

    private bool _isBusy;
    private bool _isClosing;
    public ScreenManagerWindow(
    IApplication app,
    IScreenService screenService,
    DeviceService deviceService,
    Device device)
    {
        _app = app;
        _screenService = screenService;
        _deviceService = deviceService;
        _device = device;

        var savedSettings = device.SshSettings
            ?? new SshConnectionSettings();

        _rememberPassphrase = savedSettings.RememberPassphrase;

        Title = $"ServerDesk | Sesje Screen | {device.Name}";

        _settingsButton = new Button
        {
            Text = "Lista sesji",
            X = 1,
            Y = 0,
            ShadowStyle = null
        };

        var backButton = new Button
        {
            Text = "Wroc",
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

        // Oba panele zajmuja ten sam obszar.
        // W danym momencie widoczny jest tylko jeden.
        _connectionPanel = new FrameView
        {
            Title = "Ustawienia SSH",
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ShadowStyle = null
        };

        _sessionsArea = new FrameView
        {
            Title = "Zarzadzanie sesjami",
            X = 0,
            Y = 2,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            ShadowStyle = null,
            Visible = false
        };

        string defaultKeyPath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile),
            ".ssh",
            "serverdesk_ed25519");

        var keyPathLabel = new Label
        {
            Text = "Klucz prywatny:",
            X = 1,
            Y = 0
        };

        _keyPathField = new TextField
        {
            X = 1,
            Y = 1,
            Width = Dim.Fill(2),
            Text = string.IsNullOrWhiteSpace(savedSettings.PrivateKeyPath)
                ? defaultKeyPath
                : savedSettings.PrivateKeyPath
        };

        var passphraseLabel = new Label
        {
            Text = "Passphrase:",
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
                ? "Zapamietywanie: TAK"
                : "Zapamietywanie: NIE",
            X = 1,
            Y = 4,
            ShadowStyle = null
        };

        _forgetPassphraseButton = new Button
        {
            Text = "Zapomnij haslo",
            X = Pos.Right(_rememberButton) + 2,
            Y = 4,
            ShadowStyle = null
        };

        var fingerprintLabel = new Label
        {
            Text = "Zaufany odcisk hosta SHA256:",
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
            Text = "Polacz",
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
            Text = "Nazwa nowej sesji:",
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
            Text = "Odswiez",
            X = 1,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        _createButton = new Button
        {
            Text = "Utworz sesje",
            X = Pos.Right(_refreshButton) + 2,
            Y = 3,
            ShadowStyle = null,
            Enabled = false
        };

        _previewButton = new Button
        {
            Text = "Otworz konsole",
            X = 1,
            Y = 4,
            ShadowStyle = null,
            Enabled = false
        };

        _terminateButton = new Button
        {
            Text = "Zakoncz sesje",
            X = Pos.Right(_previewButton) + 2,
            Y = 4,
            ShadowStyle = null,
            Enabled = false
        };

        var sessionsPanel = new FrameView
        {
            Title = "Sesje GNU Screen",
            X = 0,
            Y = 6,
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
            _previewButton,
            _terminateButton,
            sessionsPanel);

        _selectedSessionLabel = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(1),
            Text = "Nie wybrano sesji."
        };

        _status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = "Uzupelnij dane i kliknij Polacz."
        };

        Add(
            _settingsButton,
            backButton,
            hostLabel,
            _connectionPanel,
            _sessionsArea,
            _selectedSessionLabel,
            _status);

        _sessionList.SetSource(
            new ObservableCollection<string>(
                ["Nie pobrano jeszcze sesji."]));

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
                _status.Text = "Poczekaj na zakonczenie operacji.";
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
                ? "Zapamietywanie: TAK"
                : "Zapamietywanie: NIE";

            _status.Text = "Kliknij Polacz, aby zapisac zmiane.";
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
                    _credentialStore.Load(_device.Id);

                if (savedPassphrase is not null)
                {
                    _passphraseField.Text = savedPassphrase;
                    _status.Text =
                        "Passphrase wczytane. Kliknij Polacz.";
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

        Disposing += (_, _) =>
        {
            _isClosing = true;
        };

        _keyPathField.SetFocus();

        AppTheme.Apply(this);

        _terminateButton.SetScheme(AppTheme.CreateWarning());
        _selectedSessionLabel.SetScheme(AppTheme.CreateAccent());
    }
    private ConnectionData ReadConnectionData()
    {
        return new ConnectionData(
            _keyPathField.Text.Trim(),
            _passphraseField.Text,
            _fingerprintField.Text.Trim(),
            _rememberPassphrase);
    }

    private bool EnsureConnectionReady()
    {
        if (_verifiedConnection is not null &&
            _verifiedConnection == ReadConnectionData())
        {
            return true;
        }

        _verifiedConnection = null;

        _refreshButton.Enabled = false;
        _createButton.Enabled = false;
        _previewButton.Enabled = false;
        _terminateButton.Enabled = false;

        UpdateSessionList(Array.Empty<ScreenSession>());

        _status.Text =
            "Dane polaczenia nie zostaly zatwierdzone. Kliknij Polacz.";

        return false;
    }

    private void ShowConnectionSettings(bool showSettings)
    {
        _connectionPanel.Visible = showSettings;
        _sessionsArea.Visible = !showSettings;

        _settingsButton.Text = showSettings
            ? "Lista sesji"
            : "Ustawienia SSH";

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

                _verifiedConnection = null;
                _refreshButton.Enabled = false;
                _createButton.Enabled = false;
                _previewButton.Enabled = false;
                _terminateButton.Enabled = false;

                UpdateSessionList(Array.Empty<ScreenSession>());

                _status.Text =
                    "Zapomniano passphrase. Uzupelnij dane i kliknij Polacz.";
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


    private async Task RefreshSessionsAsync(bool connect = false)
    {
        if (_isBusy || _isClosing)
            return;

        if (!connect && !EnsureConnectionReady())
            return;

        ConnectionData connection = ReadConnectionData();

        if (connect)
        {
            // Nowa proba wymaga ponownego zatwierdzenia danych.
            _verifiedConnection = null;

            _refreshButton.Enabled = false;
            _createButton.Enabled = false;
            _previewButton.Enabled = false;
            _terminateButton.Enabled = false;

            UpdateSessionList(Array.Empty<ScreenSession>());
        }

        if (!File.Exists(connection.KeyPath))
        {
            _status.Text = "Nie znaleziono klucza prywatnego.";
            return;
        }

        if (string.IsNullOrWhiteSpace(connection.Fingerprint))
        {
            _status.Text = "Podaj odcisk SHA256 hosta.";
            return;
        }

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
            ? "Sprawdzanie dostepu SSH..."
            : "Odswiezanie listy sesji...";

        try
        {
            var sessions = await Task.Run(
                () => _screenService.GetSessionsAsync(
                    _device,
                    connection.KeyPath,
                    connection.Passphrase,
                    connection.Fingerprint));

            // Ustawienia zapisujemy tylko po kliknieciu Polacz.
            if (connect)
            {
                await SaveConnectionSettingsAsync(
                    connection.KeyPath,
                    connection.Fingerprint,
                    connection.Passphrase);
            }

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (connect)
                    _verifiedConnection = connection;

                UpdateSessionList(sessions);

                if (connect)
                    ShowConnectionSettings(false);

                _status.Text = connect
                    ? $"Dostep SSH zweryfikowany | Sesje: {sessions.Count}"
                    : $"Lista odswiezona | Sesje: {sessions.Count}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                // Blad odswiezania nie zmienia zatwierdzonych danych.
                // Mozna ponowic odswiezenie.
                _status.Text = connect
                    ? $"Blad polaczenia: {ex.Message}"
                    : $"Blad odswiezania: {ex.Message}";
            });
        }
        finally
        {
            _app.Invoke(() =>
            {
                _isBusy = false;

                if (_isClosing)
                    return;

                bool verified = _verifiedConnection is not null;

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
            _status.Text = "Najpierw wybierz sesje.";
            return;
        }

        string keyPath = _keyPathField.Text.Trim();
        string passphrase = _passphraseField.Text;
        string fingerprint = _fingerprintField.Text.Trim();
        string sessionId = _sessions[index].Id;

        if (!File.Exists(keyPath))
        {
            _status.Text = "Nie znaleziono klucza SSH.";
            return;
        }

        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            _status.Text = "Podaj odcisk SHA256 hosta.";
            return;
        }

        _isBusy = true;

        try
        {
            using var consoleWindow = new ScreenConsoleWindow(
                _app,
                _screenService,
                _device,
                sessionId,
                keyPath,
                passphrase,
                fingerprint);

            _app.Run(consoleWindow);

            _status.Text = $"Powrot z konsoli: {sessionId}";
        }
        catch (Exception ex)
        {
            _status.Text = $"Blad otwierania konsoli: {ex.Message}";
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
            _status.Text = "Najpierw wybierz sesje.";
            return;
        }

        // Zapamietujemy dokladnie te sesje i dane,
        // dla ktorych wyswietlimy potwierdzenie.
        string sessionId = _sessions[index].Id;
        ConnectionData connection = ReadConnectionData();

        bool terminationRequested = false;

        _isBusy = true;

        try
        {
            int? answer = MessageBox.Query(
                _app,
                "Zakonczenie sesji",
                $"Urzadzenie: {_device.Name}\n" +
                $"Host: {_device.Host}:{_device.Port}\n" +
                $"Sesja: {sessionId}\n\n" +
                "Zakonczenie sesji zamknie wszystkie jej okna " +
                "i moze przerwac uruchomione programy.\n\n" +
                "Czy kontynuowac?",
                "Anuluj",
                "Zakoncz sesje");

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

            _status.Text = $"Konczenie sesji: {sessionId}";

            var sessions = await Task.Run(async () =>
            {
                await _screenService.TerminateSessionAsync(
                    _device,
                    connection.KeyPath,
                    connection.Passphrase,
                    connection.Fingerprint,
                    sessionId);

                terminationRequested = true;

                return await _screenService.GetSessionsAsync(
                    _device,
                    connection.KeyPath,
                    connection.Passphrase,
                    connection.Fingerprint);
            });

            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                UpdateSessionList(sessions);

                bool stillListed = sessions.Any(s => s.Id == sessionId);

                _status.Text = stillListed
                    ? "Wyslano zadanie zakonczenia. Odswiez liste za chwile."
                    : $"Sesja zakonczona: {sessionId}";
            });
        }
        catch (Exception ex)
        {
            _app.Invoke(() =>
            {
                if (_isClosing)
                    return;

                if (terminationRequested)
                {
                    // Zadanie wyslano, ale odczyt listy sie nie powiodl.
                    // Nie zostawiamy starej listy jako aktualnej.
                    UpdateSessionList(Array.Empty<ScreenSession>());

                    _status.Text =
                        "Wyslano zadanie zakonczenia, ale nie odswiezono " +
                        $"listy: {ex.Message}";
                }
                else
                {
                    _status.Text =
                        $"Blad operacji: {ex.Message}. Odswiez liste, " +
                        "aby sprawdzic stan sesji.";
                }
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
                    _verifiedConnection is not null &&
                    _verifiedConnection == ReadConnectionData();

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
            await _screenService.CreateSessionAsync(
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
