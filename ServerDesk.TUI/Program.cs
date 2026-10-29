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
var screenService = new SshScreenService();

var devices = await deviceService.GetDevicesAsync();

using IApplication app = Application.Create().Init();

using var window = new MainWindow(
    app,
    deviceService,
    devices,
    screenService);

app.Run(window);