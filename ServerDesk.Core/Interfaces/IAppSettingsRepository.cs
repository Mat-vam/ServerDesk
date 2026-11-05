using ServerDesk.Core.Models;

namespace ServerDesk.Core.Interfaces;

public interface IAppSettingsRepository
{
    Task<AppSettings> LoadAsync(
        CancellationToken cancellationToken = default);

    Task SaveAsync(
        AppSettings settings,
        CancellationToken cancellationToken = default);
}