using ServerDesk.Core.Interfaces;
using ServerDesk.Core.Models;
using ServerDesk.Core.Errors;

namespace ServerDesk.Core.Services;

public sealed class ScreenConsoleService
{
    private readonly IScreenService _screenService;

    public ScreenConsoleService(IScreenService screenService)
    {
        _screenService = screenService;
    }

    public async Task<ConsoleOperationResult> RefreshAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        string output = await _screenService.GetConsoleOutputAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            sessionId,
            cancellationToken);

        return new ConsoleOperationResult(
            Output: output,
            CommandSent: false);
    }

    public async Task<ConsoleOperationResult> SendAndRefreshAsync(
        Device device,
        string keyPath,
        string passphrase,
        string fingerprint,
        string sessionId,
        string commandText,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandText) ||
            commandText.Length > 512 ||
            commandText.Any(char.IsControl))
        {
            throw new AppException(AppErrorCode.InvalidCommand);
        }

        await _screenService.SendCommandAsync(
            device,
            keyPath,
            passphrase,
            fingerprint,
            sessionId,
            commandText,
            cancellationToken);

        // Błąd odświeżenia zachowuje informację o wysłaniu polecenia.
        try
        {
            await Task.Delay(250, cancellationToken);

            string output =
                await _screenService.GetConsoleOutputAsync(
                    device,
                    keyPath,
                    passphrase,
                    fingerprint,
                    sessionId,
                    cancellationToken);

            return new ConsoleOperationResult(
                Output: output,
                CommandSent: true);
        }
        catch (Exception ex)
        {
            return new ConsoleOperationResult(
                Output: null,
                CommandSent: true,
                RefreshError: ex);
        }
    }
}