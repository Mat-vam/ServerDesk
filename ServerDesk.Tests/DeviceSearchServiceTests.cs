using ServerDesk.Core.Models;
using ServerDesk.Core.Services;

namespace ServerDesk.Tests;

public class DeviceSearchServiceTests
{
    private static List<Device> CreateDevices()
    {
        return new List<Device>
        {
            new()
            {
                Name = "Minecraft Server",
                Host = "192.168.1.100",
                Username = "admin"
            },
            new()
            {
                Name = "Raspberry Pi",
                Host = "192.168.1.120",
                Username = "pi"
            },
            new()
            {
                Name = "Backup Server",
                Host = "nas.local",
                Username = "backup"
            }
        };
    }

    [Fact]
    public void Search_ShouldIgnoreCase()
    {
        var devices = CreateDevices();

        var results = DeviceSearchService.FindMatches(
            devices, "MINECRAFT");

        Assert.Single(results);
        Assert.Equal(devices[0].Id, results[0]);
    }

    [Fact]
    public void Search_ShouldFindByIp()
    {
        var devices = CreateDevices();

        var results = DeviceSearchService.FindMatches(
            devices, "192.168.1.120");

        Assert.Single(results);
        Assert.Equal(devices[1].Id, results[0]);
    }

    [Fact]
    public void Search_ShouldReturnEmptyForUnknownDevice()
    {
        var devices = CreateDevices();

        var results = DeviceSearchService.FindMatches(
            devices, "unknown");

        Assert.Empty(results);
    }

    [Fact]
    public void Search_ShouldHandleEmptyQuery()
    {
        var devices = CreateDevices();

        var results = DeviceSearchService.FindMatches(
            devices, "  ");

        Assert.Empty(results);
    }
}
