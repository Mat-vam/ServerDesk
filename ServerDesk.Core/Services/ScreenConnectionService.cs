using ServerDesk.Core.Models;
using ServerDesk.Core.Errors;

namespace ServerDesk.Core.Services;

public sealed class ScreenConnectionService
{
    private readonly ScreenManagerService _screenManager;
    private readonly SshSettingsService _settingsService;
    private readonly Device _device;

    private ScreenConnectionData? _verifiedConnection;

    internal ScreenConnectionService(
        ScreenManagerService screenManager,
        SshSettingsService settingsService,
        Device device)
    {
        _screenManager = screenManager;
        _settingsService = settingsService;
        _device = device;
    }

    private static ScreenConnectionData Normalize(
        ScreenConnectionData data)
    {
        return data with
        {
            KeyPath = data.KeyPath.Trim(),
            Fingerprint = data.Fingerprint.Trim()
        };
    }

    private static void Validate(ScreenConnectionData data)
    {
        if (string.IsNullOrWhiteSpace(data.KeyPath))
        {
            throw new AppException(AppErrorCode.PrivateKeyPathRequired);
        }

        if (string.IsNullOrWhiteSpace(data.Fingerprint))
        {
            throw new AppException(AppErrorCode.HostFingerprintRequired);
        }

        // Otwieranie pliku klucza i sprawdzenie klucza hosta
        // pozostaja w implementacji SSH.
    }

    public void Invalidate()
    {
        _verifiedConnection = null;
    }

    public bool IsReady(ScreenConnectionData current)
    {
        if (_verifiedConnection is not null &&
            _verifiedConnection == Normalize(current))
        {
            return true;
        }

        Invalidate();
        return false;
    }

    public ScreenConnectionData GetVerifiedConnection(
        ScreenConnectionData current)
    {
        if (!IsReady(current))
        {
            throw new AppException(AppErrorCode.ConnectionSettingsChanged);
        }

        return _verifiedConnection!;
    }

    public async Task<IReadOnlyList<ScreenSession>> ConnectAsync(
        ScreenConnectionData input,
        CancellationToken cancellationToken = default)
    {
        Invalidate();

        ScreenConnectionData connection = Normalize(input);
        Validate(connection);

        var sessions = await _screenManager.GetSessionsAsync(
            _device,
            connection.KeyPath,
            connection.Passphrase,
            connection.Fingerprint,
            cancellationToken);

        // Zapis nastepuje dopiero po udanej operacji SSH.
        await _settingsService.SaveAsync(
            _device,
            connection.KeyPath,
            connection.Fingerprint,
            connection.Passphrase,
            connection.RememberPassphrase,
            cancellationToken);

        _verifiedConnection = connection;

        return sessions;
    }

    public Task<IReadOnlyList<ScreenSession>> RefreshAsync(
        ScreenConnectionData input,
        CancellationToken cancellationToken = default)
    {
        var connection = GetVerifiedConnection(input);

        return _screenManager.GetSessionsAsync(
            _device,
            connection.KeyPath,
            connection.Passphrase,
            connection.Fingerprint,
            cancellationToken);
    }

    public Task<ScreenSessionsResult> CreateSessionAsync(
        ScreenConnectionData input,
        string sessionName,
        CancellationToken cancellationToken = default)
    {
        var connection = GetVerifiedConnection(input);

        return _screenManager.CreateAndRefreshAsync(
            _device,
            connection.KeyPath,
            connection.Passphrase,
            connection.Fingerprint,
            sessionName.Trim(),
            cancellationToken);
    }

    public Task<ScreenSessionsResult> TerminateSessionAsync(
        ScreenConnectionData input,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var connection = GetVerifiedConnection(input);

        return _screenManager.TerminateAndRefreshAsync(
            _device,
            connection.KeyPath,
            connection.Passphrase,
            connection.Fingerprint,
            sessionId,
            cancellationToken);
    }

    public SshConnectionSettings GetInitialSettings()
    {
        var saved = _device.SshSettings
            ?? new SshConnectionSettings();

        string keyPath = saved.PrivateKeyPath;

        if (string.IsNullOrWhiteSpace(keyPath))
        {
            keyPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.UserProfile),
                ".ssh",
                "serverdesk_ed25519");
        }

        return new SshConnectionSettings
        {
            PrivateKeyPath = keyPath,
            HostFingerprint = saved.HostFingerprint,
            RememberPassphrase = saved.RememberPassphrase,
            AutoConnect = saved.AutoConnect
        };
    }

    public string? LoadSavedPassphrase()
    {
        return _settingsService.LoadSavedPassphrase(_device);
    }

    public async Task ForgetPassphraseAsync(
        CancellationToken cancellationToken = default)
    {
        Invalidate();

        await _settingsService.ForgetPassphraseAsync(
            _device,
            cancellationToken);
    }
}