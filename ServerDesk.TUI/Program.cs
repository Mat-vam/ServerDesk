
using ServerDesk.Core.Services;
using ServerDesk.Infrastructure.SSH;
using ServerDesk.Infrastructure.Storage;
using ServerDesk.TUI.Views;
using Terminal.Gui.App;

string appDataPath = Path.Combine(
    Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),
    "ServerDesk");

string devicesFile = Path.Combine(
    appDataPath,
    "devices.json");

var repository = new JsonDeviceRepository(devicesFile);
var deviceService = new DeviceService(repository);

var devices = await deviceService.GetDevicesAsync();

using IApplication app = Application.Create().Init();

if (args.Contains("--screens"))
{
    if (devices.Count == 0)
    {
        Console.WriteLine(
            "Brak urzadzen. Dodaj najpierw komputer.");

        return;
    }

    // Tymczasowo testujemy pierwsze zapisane urzadzenie.
    // Pozniej bedziemy wybierac je w interfejsie.
    var device = devices[0];

    var screenService = new SshScreenService();

    using var window = new ScreenManagerWindow(app, screenService, deviceService, device);

    app.Run(window);
}
else
{
    using var window = new MainWindow(
        app,
        deviceService,
        devices);

    app.Run(window);
}
