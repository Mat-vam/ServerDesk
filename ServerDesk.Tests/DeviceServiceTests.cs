using ServerDesk.Core.Models;
using ServerDesk.Core.Services;
using ServerDesk.Infrastructure.Storage;
namespace ServerDesk.Tests;

public class DeviceServiceTests
{
    [Fact]
    public async Task AddDevice_ShouldSaveDevice()
    {
        string filePath = Path.Combine(Path.GetTempPath(),
            $"serverdesk-test-{Guid.NewGuid():N}.json");

        try
        {
            var repository = new JsonDeviceRepository(filePath);
            var service = new DeviceService(repository);

            await service.AddDeviceAsync("Test Server", "192.168.1.100", 22, "admin");

            var devices = await service.GetDevicesAsync();

            var device = Assert.Single(devices);

            Assert.Equal("Test Server", device.Name);
            Assert.Equal("192.168.1.100", device.Host);
            Assert.Equal(22, device.Port);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public async Task UpdateDevice_ShouldChangeSavedData()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"serverdesk-test-{Guid.NewGuid():N}.json");

        try
        {
            var repository = new JsonDeviceRepository(filePath);
            var service = new DeviceService(repository);

            var device = await service.AddDeviceAsync(
                "Old Name",
                "192.168.1.100",
                22,
                "admin");

            bool result = await service.UpdateDeviceAsync(
                device.Id,
                "New Name",
                "192.168.1.200",
                2222,
                "root");

            Assert.True(result);

            var devices = await service.GetDevicesAsync();
            var updated = Assert.Single(devices);

            Assert.Equal("New Name", updated.Name);
            Assert.Equal("192.168.1.200", updated.Host);
            Assert.Equal(2222, updated.Port);
            Assert.Equal("root", updated.Username);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }


    [Fact]
    public async Task UpdateSshSettings_ShouldPersistSettings()
    {
        string filePath = Path.Combine(
            Path.GetTempPath(),
            $"serverdesk-ssh-{Guid.NewGuid():N}.json");

        try
        {
            var repository = new JsonDeviceRepository(filePath);
            var service = new DeviceService(repository);

            var device = await service.AddDeviceAsync(
                "Raspberry Pi",
                "192.168.1.120",
                22,
                "pi");

            var settings = new SshConnectionSettings
            {
                PrivateKeyPath = @"C:\Keys\raspberry_ed25519",
                HostFingerprint = "SHA256:TEST_FINGERPRINT",
                RememberPassphrase = false,
                AutoConnect = false
            };

            bool updated = await service.UpdateSshSettingsAsync(
                device.Id,
                settings);

            Assert.True(updated);

            // Nowa instancja sprawdza faktyczny zapis do pliku.
            var newRepository = new JsonDeviceRepository(filePath);
            var newService = new DeviceService(newRepository);

            var devices = await newService.GetDevicesAsync();
            var saved = Assert.Single(devices);

            Assert.Equal(
                @"C:\Keys\raspberry_ed25519",
                saved.SshSettings.PrivateKeyPath);

            Assert.Equal(
                "SHA256:TEST_FINGERPRINT",
                saved.SshSettings.HostFingerprint);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }


}
