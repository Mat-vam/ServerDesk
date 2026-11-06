using ServerDesk.Bootstrap;
using ServerDesk.TUI.Localization;
using ServerDesk.TUI.Views;
using Terminal.Gui.App;

var services = new ServerDeskServices();

UiText.SetLanguage(await services.Settings.GetLanguageAsync());

var devices = await services.Devices.GetDevicesAsync();

using IApplication app = Application.Create().Init();

using var window = new MainWindow(
    app,
    services.Devices,
    devices,
    services.Screens,
    services.Console,
    services.SshSettings,
    services.Settings);

app.Run(window);