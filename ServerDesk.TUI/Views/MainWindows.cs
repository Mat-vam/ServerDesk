using ServerDesk.TUI.Styling;
using System.Collections.ObjectModel;
using ServerDesk.Core.Models;
using ServerDesk.Core.Services;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui;
using ServerDesk.TUI.Components;
using Terminal.Gui.Input;
using ServerDesk.Core.Interfaces;
using ServerDesk.TUI.Localization;

namespace ServerDesk.TUI.Views;

public sealed class MainWindow : Window
{
    private readonly AppSettingsService _appSettingsService;
    private readonly Button _languageButton;

    private readonly IApplication _app;
    private readonly DeviceService _deviceService;
    private readonly ScreenManagerService _screenService;
    private readonly ScreenConsoleService _consoleService;
    private readonly SshSettingsService _sshSettingsService;
    private readonly Button _screensButton;
    private readonly List<Device> _devices;

    private readonly ListView _deviceList;
    private readonly TextField _searchField;
    private readonly Label _counter;
    private readonly Label _status;

    private readonly FrameView _leftPanel;
    private readonly FrameView _rightPanel;
    private readonly FrameView _actionPanel;
    private readonly FrameView _searchPanel;
    private readonly FrameView _deviceListPanel;
    private bool _compactLayout;
    private bool _layoutInitialized;
    private const int CompactBreakpoint = 80;

    private readonly TextField _nameField;
    private readonly TextField _hostField;
    private readonly TextField _portField;
    private readonly TextField _usernameField;

    private readonly Button _newButton;
    private readonly Button _saveButton;
    private readonly Button _deleteButton;
    private readonly Button _searchButton;

    private Guid? _editingId;

    private bool _suppressSelectionChanged;
    private bool _isNavigating;
    private int _displayedIndex = -1;
    private FormSnapshot _originalForm = new("", "", "22", "");

    private sealed record FormSnapshot(
        string Name,
        string Host,
        string Port,
        string Username
    );


    public MainWindow(
        IApplication app,
        DeviceService deviceService,
        IEnumerable<Device> devices,
        ScreenManagerService screenService,
        ScreenConsoleService consoleService,
        SshSettingsService sshSettingsService,
        AppSettingsService appSettingsService)
    {
        _app = app;
        _deviceService = deviceService;
        _screenService = screenService;
        _appSettingsService = appSettingsService;
        _consoleService = consoleService;
        _sshSettingsService = sshSettingsService;
        _devices = devices.ToList();

        Title = UiText.Get("MainWindowTitle");

        // Panel wyszukiwania i listy urządzeń.
        _leftPanel = new FrameView
        {
            Title = UiText.Get("Devices"),
            X = 0,
            Y = 0,
            Width = Dim.Percent(35),
            Height = Dim.Fill(2),

            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        _searchPanel = new FrameView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = 6,
            CanFocus = true,
            TabStop = TabBehavior.NoStop,
            ShadowStyle = null
        };

        var searchLabel = new Label
        {
            Text = UiText.Get("SearchLabel"),
            X = 0,
            Y = 0
        };

        _searchField = new TextField
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        _searchButton = new Button
        {
            Text = UiText.Get("FindNext"),
            X = 0,
            Y = 2,
            CanFocus = true,
            TabStop = TabBehavior.TabStop,
            ShadowStyle = null
        };

        _searchPanel.Add(
            searchLabel,
            _searchField,
            _searchButton
        );

        _deviceListPanel = new FrameView
        {
            X = 0,
            Y = Pos.Bottom(_searchPanel),
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = true,
            TabStop = TabBehavior.NoStop,
            ShadowStyle = null
        };

        _deviceList = new DeviceListView
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        _deviceList.FrameChanged += (_, _) =>
        {
            _deviceList.SetNeedsDraw();
        };

        _counter = new Label
        {
            X = 0,
            Y = Pos.Bottom(_deviceList),
            Width = Dim.Fill(),
            Text = ""
        };

        _deviceListPanel.Add(
            _deviceList,
            _counter
        );

        _leftPanel.Add(
            _searchPanel,
            _deviceListPanel
        );

        // Formularz urządzenia.
        _rightPanel = new FrameView
        {
            Title = UiText.Get("DeviceDetails"),
            X = Pos.Right(_leftPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),

            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var nameLabel = new Label
        {
            Text = UiText.Get("NameLabel"),
            X = 1,
            Y = 1
        };

        _nameField = CreateField(2);

        var hostLabel = new Label
        {
            Text = UiText.Get("HostLabel"),
            X = 1,
            Y = 4
        };

        _hostField = CreateField(5);

        var portLabel = new Label
        {
            Text = UiText.Get("SshPortLabel"),
            X = 1,
            Y = 7
        };

        _portField = CreateField(8);
        _portField.Text = "22";

        var usernameLabel = new Label
        {
            Text = UiText.Get("SshUserLabel"),
            X = 1,
            Y = 10
        };

        _usernameField = CreateField(11);

        // Panel akcji.
        _actionPanel = new FrameView
        {
            Title = UiText.Get("Actions"),
            X = 0,
            Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(),
            Height = 3,
            ShadowStyle = null
        };

        _newButton = new Button
        {
            Text = UiText.Get("New"),
            X = 1,
            Y = 0,
            ShadowStyle = null
        };

        _saveButton = new Button
        {
            Text = UiText.Get("Save"),
            X = Pos.Right(_newButton) + 2,
            Y = 0,
            ShadowStyle = null
        };

        _deleteButton = new Button
        {
            Text = UiText.Get("Delete"),
            X = Pos.Right(_saveButton) + 2,
            Y = 0,
            ShadowStyle = null
        };

        _screensButton = new Button
        {
            Text = UiText.Get("ScreenSessions"),
            X = Pos.Right(_deleteButton) + 2,
            Y = 0,
            ShadowStyle = null,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        _actionPanel.Add(_newButton, _saveButton, _deleteButton, _screensButton);

        _newButton.CanFocus = true;
        _newButton.TabStop = TabBehavior.TabStop;

        _saveButton.CanFocus = true;
        _saveButton.TabStop = TabBehavior.TabStop;

        _deleteButton.CanFocus = true;
        _deleteButton.TabStop = TabBehavior.TabStop;

        _rightPanel.Add(
            nameLabel,
            _nameField,
            hostLabel,
            _hostField,
            portLabel,
            _portField,
            usernameLabel,
            _usernameField);

        _status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = UiText.Get("Ready")
        };

        _languageButton = new Button
        {
            Text = UiText.Get("LanguageButton"),
            X = 1,
            Y = Pos.AnchorEnd(2),
            ShadowStyle = null,
            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };

        Add(
            _leftPanel,
            _rightPanel,
            _actionPanel,
            _languageButton,
            _status);

        KeyboardNavigation.Configure(
            this,
            _searchField,
            _searchButton,
            _deviceList,
            _nameField,
            _hostField,
            _portField,
            _usernameField,
            _newButton,
            _saveButton,
            _deleteButton,
            _screensButton,
            _languageButton);

        _searchField.KeyDown += (_, key) =>
        {
            if (key == Key.Enter)
            {
                key.Handled = true;
                FindNextDevice();
            }
        };

        RegisterEvents();

        RefreshDeviceList();

        if (_devices.Count > 0)
        {
            ShowSelectedDevice();
        }
        else
        {
            ClearForm();
        }

        if (_devices.Count > 0)
        {
            _deviceList.SetFocus();
        }
        else
        {
            _nameField.SetFocus();
        }

        FrameChanged += (_, _) =>
        {
            UpdateResponsiveLayout();
        };

        UpdateResponsiveLayout();

        AppTheme.Apply(this);

        _deleteButton.SetScheme(AppTheme.CreateWarning());
        _counter.SetScheme(AppTheme.CreateAccent());
    }


    private void RegisterEvents()
    {
        _languageButton.Accepted += async (_, _) =>
        {
            await ChangeLanguageAsync();
        };

        _screensButton.Accepted += async (_, _) =>
        {
            await OpenScreenManagerAsync();
        };

        _deviceList.ValueChanged += async (_, _) =>
        {
            await HandleDeviceSelectionChangedAsync();
        };

        _newButton.Accepted += async (_, _) =>
        {
            if (_isBusy || _isNavigating)
                return;

            _isNavigating = true;

            try
            {
                bool canLeave = await ConfirmDiscardChangesAsync();

                if (canLeave)
                    ClearForm();
            }
            finally
            {
                _isNavigating = false;
            }
        };

        _saveButton.Accepted += async (_, _) =>
        {
            await SaveDeviceAsync();
        };

        _deleteButton.Accepted += async (_, _) =>
        {
            await DeleteDeviceAsync();
        };

        _searchButton.Accepted += (_, _) =>
        {
            FindNextDevice();
        };
    }


    private void UpdateResponsiveLayout()
    {
        int availableWidth = Frame.Width;

        if (availableWidth <= 0)
            return;

        bool shouldBeCompact =
            availableWidth < CompactBreakpoint;

        if (_layoutInitialized &&
            shouldBeCompact == _compactLayout)
            return;

        _layoutInitialized = true;
        _compactLayout = shouldBeCompact;

        if (_compactLayout)
        {
            // Układ pionowy z listą nad formularzem.
            _leftPanel.X = 0;
            _leftPanel.Y = 0;
            _leftPanel.Width = Dim.Fill();
            _leftPanel.Height = Dim.Percent(40);

            _rightPanel.X = 0;
            _rightPanel.Y = Pos.Bottom(_leftPanel);
            _rightPanel.Width = Dim.Fill();
            _rightPanel.Height = Dim.Fill(5);
        }

        else
        {
            // Układ poziomy z listą obok formularza.
            _leftPanel.X = 0;
            _leftPanel.Y = 0;
            _leftPanel.Width = Dim.Percent(35);
            _leftPanel.Height = Dim.Fill(5);

            _rightPanel.X = Pos.Right(_leftPanel);
            _rightPanel.Y = 0;
            _rightPanel.Width = Dim.Fill();
            _rightPanel.Height = Dim.Fill(5);
        }
    }

    private async Task ChangeLanguageAsync()
    {
        if (_isBusy || _isNavigating)
            return;

        _isNavigating = true;

        try
        {
            int? answer = MessageBox.Query(
                _app,
                UiText.Get("LanguageTitle"),
                UiText.Get("LanguagePrompt"),
                UiText.Get("LanguageEnglish"),
                UiText.Get("LanguagePolish"),
                UiText.Get("Cancel"));

            if (answer is not (0 or 1))
                return;

            string language = answer == 0 ? "en" : "pl";

            _isBusy = true;
            _languageButton.Enabled = false;

            await _appSettingsService.SetLanguageAsync(language);

            _status.Text = UiText.Get("LanguageSavedRestart");

            MessageBox.Query(
                _app,
                UiText.Get("LanguageTitle"),
                UiText.Get("LanguageSavedRestart"),
                UiText.Get("OK"));
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            _isBusy = false;
            _isNavigating = false;
            _languageButton.Enabled = true;
        }
    }

    private void FindNextDevice()
    {
        if (_isBusy || _isNavigating)
            return;

        string query = _searchField.Text;

        if (string.IsNullOrWhiteSpace(query))
        {
            _status.Text = UiText.Get("EnterSearchText");
            return;
        }

        int currentIndex = _deviceList.Value is int index
            ? index
            : -1;

        Guid? currentDeviceId =
            currentIndex >= 0 && currentIndex < _devices.Count
                ? _devices[currentIndex].Id
                : null;

        Guid? targetId = DeviceSearchService.FindNextMatch(
            _devices,
            query,
            currentDeviceId);

        if (targetId is not Guid foundId)
        {
            _status.Text = UiText.Format("NoSearchResults", query); ;
            return;
        }

        // Dopasowanie identyfikatora urządzenia do pozycji na liście.
        int targetIndex = _devices.FindIndex(d => d.Id == foundId);

        if (targetIndex < 0)
            return;

        _deviceList.Value = targetIndex;

        if (targetIndex == currentIndex)
        {
            _status.Text = UiText.Format(
                "OnlyMatchingDevice",
                _devices[targetIndex].Name);
        }
    }
    private FormSnapshot GetFormSnapshot()
    {
        return new FormSnapshot(
            _nameField.Text,
            _hostField.Text,
            _portField.Text,
            _usernameField.Text
        );
    }

    private void RememberFormState()
    {
        _originalForm = GetFormSnapshot();
    }

    private bool HasUnsavedChanges()
    {
        return GetFormSnapshot() != _originalForm;
    }
    private void SelectDeviceSilently(int index)
    {
        if (index < 0 || index >= _devices.Count)
            return;

        _suppressSelectionChanged = true;

        try
        {
            _deviceList.Value = index;
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }


    private async Task HandleDeviceSelectionChangedAsync()
    {
        if (_suppressSelectionChanged || _isNavigating)
            return;

        if (_deviceList.Value is not int targetIndex ||
            targetIndex < 0 ||
            targetIndex >= _devices.Count)
            return;

        if (targetIndex == _displayedIndex)
            return;

        Guid targetId = _devices[targetIndex].Id;

        _isNavigating = true;

        try
        {
            // Przywrócenie zaznaczenia na czas potwierdzenia.
            if (_displayedIndex >= 0)
            {
                SelectDeviceSilently(_displayedIndex);
            }

            bool canLeave = await ConfirmDiscardChangesAsync();

            if (!canLeave)
                return;

            // Identyfikator urządzenia pozostaje ważny po odświeżeniu listy.
            int newIndex = _devices.FindIndex(
                d => d.Id == targetId
            );

            if (newIndex < 0)
            {
                _status.Text = UiText.Get("SelectedDeviceMissing");
                return;
            }

            SelectDeviceSilently(newIndex);
            ShowSelectedDevice();
        }
        finally
        {
            _isNavigating = false;
        }
    }


    private async Task OpenScreenManagerAsync()
    {
    if (_isBusy || _isNavigating)
        return;

    _isNavigating = true;

    try
    {
        bool canLeave = await ConfirmDiscardChangesAsync();

        if (!canLeave)
            return;

        if (_editingId is not Guid deviceId)
        {
            _status.Text = UiText.Get("SelectOrSaveDevice");
            return;
        }

        int index = _devices.FindIndex(d => d.Id == deviceId);

        if (index < 0)
        {
            _status.Text = UiText.Get("DeviceNotFound");
            return;
        }

        // Przywrócenie zapisanych danych po odrzuceniu zmian.
        SelectDeviceSilently(index);
        ShowSelectedDevice();

        Device device = _devices[index];

        using (var screenWindow = new ScreenManagerWindow(
            _app,
            _screenService,
            _sshSettingsService,
            device,
            _consoleService))
        {
            _app.Run(screenWindow);
        }

        // Ponowny odczyt ustawień SSH po powrocie z menedżera sesji.
        await ReloadDevicesAsync(deviceId);

        _status.Text = UiText.Format(
            "ReturnedFromSessions",
            device.Name);
        }
    catch (Exception ex)
    {
        ShowError(ex);
    }
    finally
    {
        _isNavigating = false;
    }
}

    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!HasUnsavedChanges())
            return true;

        int? answer = MessageBox.Query(
            _app,
            UiText.Get("UnsavedChangesTitle"),
            UiText.Get("UnsavedChangesPrompt"),
            UiText.Get("Save"),
            UiText.Get("Discard"),
            UiText.Get("Cancel"));

        switch (answer)
        {
            case 0:
                return await SaveDeviceAsync();

            case 1:
                return true;

            default:
                return false;
        }
    }


    private static TextField CreateField(int y)
    {
        return new TextField
        {
            X = 1,
            Y = y,
            Width = Dim.Fill(2),

            CanFocus = true,
            TabStop = TabBehavior.TabStop
        };
    }


    private void RefreshDeviceList(Guid? selectId = null)
    {
        _suppressSelectionChanged = true;

        try
        {
            var names = new ObservableCollection<string>(
                _devices.Select(d => d.Name)
            );

            _deviceList.SetSource(names);

            _counter.Text = UiText.Format("DeviceCount", _devices.Count);

            if (_devices.Count == 0)
            {
                ClearForm();
                return;
            }

            int index = selectId.HasValue
                ? _devices.FindIndex(d => d.Id == selectId.Value)
                : 0;

            if (index < 0)
                index = 0;

            _deviceList.Value = index;
            ShowSelectedDevice();
        }
        finally
        {
            _suppressSelectionChanged = false;
        }
    }


    private Device? GetSelectedDevice()
    {
        if (_deviceList.Value is not int index)
        {
            return null;
        }

        if (index < 0 || index >= _devices.Count)
        {
            return null;
        }

        return _devices[index];
    }
    private void ShowSelectedDevice()
    {
        if (_deviceList.Value is not int index || index < 0 || index >= _devices.Count)
        {
            return;
        }

        var device = _devices[index];

        _rightPanel.Title = UiText.Format("EditingDevice", device.Name);

        _displayedIndex = index;
        _editingId = device.Id;

        _nameField.Text = device.Name;
        _hostField.Text = device.Host;
        _portField.Text = device.Port.ToString();
        _usernameField.Text = device.Username;

        RememberFormState();

        _status.Text = UiText.Format("SelectedDevice", device.Name);
    }


    private void ClearForm()
    {
        _editingId = null;
        _displayedIndex = -1;

        _nameField.Text = "";
        _hostField.Text = "";
        _portField.Text = "22";
        _usernameField.Text = "";

        RememberFormState();

        _rightPanel.Title = UiText.Get("NewDevice");

        _status.Text = UiText.Get("NewDeviceHint");
        _nameField.SetFocus();
    }

    private bool _isBusy;


    private async Task<bool> SaveDeviceAsync()
    {
        if (_isBusy)
            return false;

        string name = _nameField.Text;
        string host = _hostField.Text;
        string username = _usernameField.Text;

        if (!int.TryParse(_portField.Text, out int port))
        {
            ShowError(UiText.Get("PortMustBeNumber"));
            return false;
        }

        _isBusy = true;
        _status.Text = UiText.Get("SavingDevice");

        try
        {
            Guid savedId = await _deviceService.SaveDeviceAsync(
                _editingId,
                name,
                host,
                port,
                username);

            // Wstrzymanie zdarzeń zaznaczenia podczas odświeżania listy.
            _suppressSelectionChanged = true;

            try
            {
                await ReloadDevicesAsync(savedId);
            }
            finally
            {
                _suppressSelectionChanged = false;
            }

            RememberFormState();

            _status.Text = UiText.Get("ChangesSaved");
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex);
            return false;
        }
        finally
        {
            _isBusy = false;
        }
    }


    private async Task DeleteDeviceAsync()
    {
        if (_isBusy)
            return;

        if (!_editingId.HasValue)
        {
            ShowError(UiText.Get("SelectDeviceFirst"));
            return;
        }

        var device = _devices.FirstOrDefault(
            d => d.Id == _editingId.Value
        );

        if (device is null)
        {
            ShowError(UiText.Get("DeviceNotFound"));
            return;
        }

        int? answer = MessageBox.Query(
            _app,
            UiText.Get("Confirmation"),
            UiText.Format("DeleteDevicePrompt", device.Name),
            UiText.Get("Delete"),
            UiText.Get("Cancel"));

        if (answer != 0)
            return;

        _isBusy = true;
        _status.Text = UiText.Get("DeletingDevice");

        try
        {
            bool removed = await _deviceService.RemoveDeviceAsync(
                device.Id
            );

            if (!removed)
            {
                ShowError(UiText.Get("DeviceAlreadyMissing"));
                return;
            }

            await ReloadDevicesAsync();

            _status.Text = UiText.Get("DeviceDeleted");
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task ReloadDevicesAsync(Guid? selectId = null)
    {
        var updated = await _deviceService.GetDevicesAsync();

        _devices.Clear();
        _devices.AddRange(updated);

        RefreshDeviceList(selectId);
    }

    private void ShowError(Exception exception)
    {
        ShowError(ErrorText.Get(exception));
    }

    private void ShowError(string message)
    {
        _status.Text = UiText.Format("ErrorDetails", message);

        MessageBox.ErrorQuery(
            _app,
            UiText.Get("ErrorTitle"),
            message,
            UiText.Get("OK"));
    }
}
