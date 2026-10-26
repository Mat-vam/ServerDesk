
using ServerDesk.Core.Models;

namespace ServerDesk.Core.Services;

public static class DeviceSearchService
{
    public static IReadOnlyList<Guid> FindMatches(IEnumerable<Device> devices, string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<Guid>();

        string search = query.Trim();

        return devices
            .Where(device =>
                device.Name.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                device.Host.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase) ||
                device.Username.Contains(
                    search,
                    StringComparison.OrdinalIgnoreCase))
            .Select(device => device.Id)
            .ToList();
    }
}
