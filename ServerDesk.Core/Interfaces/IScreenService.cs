
using ServerDesk.Core.Models;

namespace ServerDesk.Core.Interfaces;

public interface IScreenService
{
    Task<IReadOnlyList<ScreenSession>> GetSessionsAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, CancellationToken cancellationToken = default);
    Task CreateSessionAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionName, CancellationToken cancellationToken = default);
    Task SendCommandAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, string text, CancellationToken cancellationToken = default);
    Task<string> GetConsoleOutputAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, CancellationToken cancellationToken = default);
    Task TerminateSessionAsync(Device device, string privateKeyPath, string passphrase, string expectedHostFingerprint, string sessionId, CancellationToken cancellationToken = default);
}
