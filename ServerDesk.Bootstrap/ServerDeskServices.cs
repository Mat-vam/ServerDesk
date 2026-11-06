using ServerDesk.Core.Services;
using ServerDesk.Infrastructure.Security;
using ServerDesk.Infrastructure.SSH;
using ServerDesk.Infrastructure.Storage;

namespace ServerDesk.Bootstrap;

public sealed class ServerDeskServices
{
    public DeviceService Devices { get; }
    public AppSettingsService Settings { get; }
    public ScreenManagerService Screens { get; }
    public ScreenConsoleService Console { get; }
    public SshSettingsService SshSettings { get; }

    public ServerDeskServices()
    {
        string dataDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "ServerDesk");

        Devices = new DeviceService(
            new JsonDeviceRepository(
                Path.Combine(dataDirectory, "devices.json")));

        Settings = new AppSettingsService(
            new JsonAppSettingsRepository(
                Path.Combine(dataDirectory, "settings.json")));

        var ssh = new SshScreenService();

        Screens = new ScreenManagerService(ssh);
        Console = new ScreenConsoleService(ssh);

        SshSettings = new SshSettingsService(
            Devices,
            new WindowsCredentialStore());
    }
}