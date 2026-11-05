using ServerDesk.Core.Services;
using ServerDesk.Infrastructure.Security;
using ServerDesk.Infrastructure.SSH;
using ServerDesk.Infrastructure.Storage;
using ServerDesk.TUI.Localization;
using ServerDesk.TUI.Views;
using Terminal.Gui.App;

string appDataPath = Path.Combine(
    Environment.GetFolderPath(
        Environment.SpecialFolder.LocalApplicationData),
    "ServerDesk");

string devicesFile = Path.Combine(
    appDataPath,
    "devices.json");

string settingsFile = Path.Combine(
    appDataPath,
    "settings.json");

var settingsRepository =
    new JsonAppSettingsRepository(settingsFile);

var appSettingsService =
    new AppSettingsService(settingsRepository);

string language = await appSettingsService.GetLanguageAsync();

UiText.SetLanguage(language);

var repository = new JsonDeviceRepository(devicesFile);
var deviceService = new DeviceService(repository);

var screenService = new SshScreenService();

var screenManagerService =
    new ScreenManagerService(screenService);

var consoleService =
    new ScreenConsoleService(screenService);

var credentialStore = new WindowsCredentialStore();

var sshSettingsService = new SshSettingsService(
    deviceService,
    credentialStore);

var devices = await deviceService.GetDevicesAsync();

using IApplication app = Application.Create().Init();

using var window = new MainWindow(
    app,
    deviceService,
    devices,
    screenManagerService,
    consoleService,
    sshSettingsService,
    appSettingsService);

app.Run(window);