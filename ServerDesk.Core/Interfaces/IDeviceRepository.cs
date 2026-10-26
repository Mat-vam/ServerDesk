using ServerDesk.Core.Models;
namespace ServerDesk.Core.Interfaces;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IReadOnlyList<Device> devices, CancellationToken cancellationToken = default);
}