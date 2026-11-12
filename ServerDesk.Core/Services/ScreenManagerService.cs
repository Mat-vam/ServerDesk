using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;

namespace ServerDesk.Core.Services;

public sealed class ScreenManagerService
{
    private readonly IScreenService _screenService;

    public ScreenManagerService(IScreenService screenService)
    {
        _screenService = screenService;
    }

    public ScreenConnectionService CreateConnectionService(
        Device device,
        SshSettingsService settingsService)
    {
        return new ScreenConnectionService(
            this,
            settingsService,
            device);
    }

    public Task<IReadOnlyList<ScreenSession>> GetSessionsAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        CancellationToken cancellationToken = default)
    {
        return _screenService.GetSessionsAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            cancellationToken);
    }

    public async Task<ScreenSessionsResult> CreateAndRefreshAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        string sessionName,
        CancellationToken cancellationToken = default)
    {
        await _screenService.CreateSessionAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            sessionName,
            cancellationToken);

        return await RefreshAfterChangeAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            terminatedSessionId: null,
            cancellationToken: cancellationToken);
    }

    public async Task<ScreenSessionsResult> TerminateAndRefreshAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        await _screenService.TerminateSessionAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            sessionId,
            cancellationToken);

        return await RefreshAfterChangeAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            terminatedSessionId: sessionId,
            cancellationToken: cancellationToken);
    }

    private async Task<ScreenSessionsResult> RefreshAfterChangeAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        string? terminatedSessionId,
        CancellationToken cancellationToken)
    {
        // Odświeżenie listy po udanej zmianie sesji.
        try
        {
            var sessions = await _screenService.GetSessionsAsync(
                device,
                keyPath,
                passphrase,
                fingerprint,
                cancellationToken);

            bool stillListed =
                terminatedSessionId is not null &&
                sessions.Any(s => s.Id == terminatedSessionId);

            return new ScreenSessionsResult(
                Sessions: sessions,
                SessionStillListed: stillListed);
        }
        catch (Exception ex)
        {
            return new ScreenSessionsResult(
                Sessions: null,
                RefreshError: ex);
        }
    }
}