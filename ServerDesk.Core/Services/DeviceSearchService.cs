
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

    public static Guid? FindNextMatch(
        IReadOnlyList<Device> devices,
        string? query,
        Guid? currentDeviceId)
    {
        var matches = FindMatches(devices, query).ToHashSet();

        if (matches.Count == 0)
            return null;

        int currentIndex = -1;

        if (currentDeviceId is Guid currentId)
        {
            for (int i = 0; i < devices.Count; i++)
            {
                if (devices[i].Id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        // Szukamy od nastepnego urzadzenia.
        // Po dojsciu do konca wracamy na poczatek.
        for (int offset = 1; offset <= devices.Count; offset++)
        {
            int index = (currentIndex + offset) % devices.Count;

            if (matches.Contains(devices[index].Id))
                return devices[index].Id;
        }

        return null;
    }
}
