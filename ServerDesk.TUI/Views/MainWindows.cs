
using System.Collections.ObjectModel;
using ServerDesk.Core.Models;
using ServerDesk.Core.Services;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Terminal.Gui;
using ServerDesk.TUI.Components;
using Terminal.Gui.Input;

namespace ServerDesk.TUI.Views;

public sealed class MainWindow : Window
{
    private readonly IApplication _app;
    private readonly DeviceService _deviceService;
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


    public MainWindow(IApplication app, DeviceService deviceService, IEnumerable<Device> devices)
    {
        _app = app;
        _deviceService = deviceService;
        _devices = devices.ToList();

        Title = "ServerDesk | Device Manager";

        // PANEL LEWY
        _leftPanel = new FrameView
        {
            Title = "Urzadzenia",
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
            Text = "Szukaj:",
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
            Text = "Znajdz nastepne",
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

        // Osobny panel listy urzadzen
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

        _searchButton.KeyDown += (_, key) =>
        {
            if (key == Key.Tab)
            {
                _deviceList.SetFocus();
                key.Handled = true;
            }
        };

        _deviceList.KeyDown += (_, key) =>
        {
            if (key == Key.Tab.WithShift)
            {
                _searchButton.SetFocus();
                key.Handled = true;
            }
        };


        _deviceListPanel.Add(
            _deviceList,
            _counter
        );

        // Dodajemy oba panele do glownego lewego panelu
        _leftPanel.Add(
            _searchPanel,
            _deviceListPanel
        );

        // PANEL PRAWY
        _rightPanel = new FrameView
        {
            Title = "Dane urzadzenia",
            X = Pos.Right(_leftPanel),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(2),

            CanFocus = true,
            TabStop = TabBehavior.TabGroup
        };

        var nameLabel = new Label
        {
            Text = "Nazwa:",
            X = 1,
            Y = 1
        };

        _nameField = CreateField(2);

        var hostLabel = new Label
        {
            Text = "Adres IP / Host:",
            X = 1,
            Y = 4
        };

        _hostField = CreateField(5);

        var portLabel = new Label
        {
            Text = "Port SSH:",
            X = 1,
            Y = 7
        };

        _portField = CreateField(8);
        _portField.Text = "22";

        var usernameLabel = new Label
        {
            Text = "Uzytkownik SSH:",
            X = 1,
            Y = 10
        };

        _usernameField = CreateField(11);

        // PANEL DOLNY AKCJI
        _actionPanel = new FrameView
        {
            Title = "Akcje",
            X = 0,
            Y = Pos.AnchorEnd(5),
            Width = Dim.Fill(),
            Height = 3,
            ShadowStyle = null
        };

        _newButton = new Button
        {
            Text = "Nowe",
            X = 1,
            Y = 0,
            ShadowStyle = null
        };

        _saveButton = new Button
        {
            Text = "Zapisz",
            X = Pos.Right(_newButton) + 2,
            Y = 0,
            ShadowStyle = null
        };

        _deleteButton = new Button
        {
            Text = "Usun",
            X = Pos.Right(_saveButton) + 2,
            Y = 0,
            ShadowStyle = null
        };

        _actionPanel.Add(_newButton, _saveButton, _deleteButton);

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

        // PASEK STATUSU
        _status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = "Gotowe."
        };

        Add(
            _leftPanel,
            _rightPanel,
            _actionPanel,
            _status);

        // OBSLUGA ZDARZEN
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
    }

    // Zdarzenia przyciskow akcji

    private void RegisterEvents()
    {
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


    // Responsywnosc przy zmianie rozmiaru okna
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
            // Uklad pionowy - 40% lista, 60% formularz
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
            // Uklad poziomy - 35% lista, 65% formularz
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



    // Wyszukiwanie urzadzen
    private void FindNextDevice()
    {
        if (_isBusy || _isNavigating)
            return;

        string query = _searchField.Text.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            _status.Text = "Wpisz tekst do wyszukania.";
            return;
        }

        var matches = DeviceSearchService.FindMatches(
            _devices,
            query
        );

        if (matches.Count == 0)
        {
            _status.Text = $"Brak wynikow dla: {query}";
            return;
        }

        int currentIndex = _deviceList.Value is int index
            ? index
            : -1;

        // Znajdujemy pierwszy pasujacy element
        // znajdujacy sie za aktualnym zaznaczeniem.
        int targetIndex = -1;

        for (int i = currentIndex + 1; i < _devices.Count; i++)
        {
            if (matches.Contains(_devices[i].Id))
            {
                targetIndex = i;
                break;
            }
        }

        // Jesli doszlismy do konca, zaczynamy od poczatku.
        if (targetIndex == -1)
        {
            targetIndex = _devices.FindIndex(
                d => matches.Contains(d.Id)
            );
        }

        if (targetIndex < 0)
            return;

        // Zmiana Value uruchomi istniejaca obsluge
        // niezapisanych zmian.
        _deviceList.Value = targetIndex;

        if (targetIndex == currentIndex)
        {
            _status.Text =
                $"Jedyny pasujacy wynik: {_devices[targetIndex].Name}";
        }
    }


    // Funkcje do zapamietywania stanu formularza i sprawdzania zmian
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
        // Nie ustawiamy nieprawidlowego indeksu.
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


    // Funkcja do obslugi zmiany zaznaczenia urzadzenia w przypadku niezapisanych zmian
    // W taki sposób dane nie zostaną ani utracone, ani nie zostaną zapisane w złym miejscu
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
            // Przywracamy zaznaczenie przed otwarciem dialogu.
            if (_displayedIndex >= 0)
            {
                SelectDeviceSilently(_displayedIndex);
            }

            bool canLeave = await ConfirmDiscardChangesAsync();

            if (!canLeave)
                return;

            // Po zapisaniu lista mogla zostac odswiezona.
            // Dlatego szukamy urzadzenia po ID, nie indeksie.
            int newIndex = _devices.FindIndex(
                d => d.Id == targetId
            );

            if (newIndex < 0)
            {
                _status.Text = "Wybrane urzadzenie nie istnieje.";
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


    // Funkcja do potwierdzania odrzucenia zmian
    private async Task<bool> ConfirmDiscardChangesAsync()
    {
        if (!HasUnsavedChanges())
            return true;

        int? answer = MessageBox.Query(
            _app,
            "Niezapisane zmiany",
            "Masz niezapisane zmiany. Co chcesz zrobic?",
            "Zapisz",
            "Odrzuc",
            "Anuluj"
        );

        switch (answer)
        {
            case 0:
                // Zapisz zmiany przed przejsciem dalej
                return await SaveDeviceAsync();

            case 1:
                // Odrzuc zmiany
                return true;

            default:
                // Anuluj lub zamkniecie okna dialogowego
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

            _counter.Text = $"Urzadzenia: {_devices.Count}";

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

        _rightPanel.Title = $"Edycja: {device.Name}";

        _displayedIndex = index;
        _editingId = device.Id;

        _nameField.Text = device.Name;
        _hostField.Text = device.Host;
        _portField.Text = device.Port.ToString();
        _usernameField.Text = device.Username;

        RememberFormState();

        _status.Text = $"Wybrano: {device.Name}";
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

        _rightPanel.Title = "Nowe urzadzenie";

        _status.Text = "Nowe urzadzenie - wypelnij formularz.";
        _nameField.SetFocus();
    }

    private bool _isBusy;


    private async Task<bool> SaveDeviceAsync()
    {
        if (_isBusy)
            return false;

        string name = _nameField.Text.Trim();
        string host = _hostField.Text.Trim();
        string username = _usernameField.Text.Trim();

        if (!int.TryParse(_portField.Text, out int port))
        {
            ShowError("Port musi byc liczba.");
            return false;
        }

        _isBusy = true;
        _status.Text = "Zapisywanie urzadzenia...";

        try
        {
            Guid savedId;

            if (_editingId.HasValue)
            {
                savedId = _editingId.Value;

                bool updated = await _deviceService.UpdateDeviceAsync(
                    savedId, name, host, port, username
                );

                if (!updated)
                {
                    ShowError("Nie znaleziono urzadzenia.");
                    return false;
                }
            }
            else
            {
                var device = await _deviceService.AddDeviceAsync(
                    name, host, port, username
                );

                savedId = device.Id;
            }

            // To jest wewnetrzna aktualizacja listy.
            // Nie powinna wywolywac ostrzezen.
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

            _status.Text = "Zmiany zapisane pomyslnie.";
            return true;
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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
            ShowError("Najpierw wybierz urzadzenie.");
            return;
        }

        var device = _devices.FirstOrDefault(
            d => d.Id == _editingId.Value
        );

        if (device is null)
        {
            ShowError("Nie znaleziono urzadzenia.");
            return;
        }

        int? answer = MessageBox.Query(
            _app,
            "Potwierdzenie",
            $"Czy na pewno usunac '{device.Name}'?",
            "Anuluj",
            "Usun"
        );

        if (answer != 1)
            return;

        _isBusy = true;
        _status.Text = "Usuwanie urzadzenia...";

        try
        {
            bool removed = await _deviceService.RemoveDeviceAsync(
                device.Id
            );

            if (!removed)
            {
                ShowError("Urzadzenie juz nie istnieje.");
                return;
            }

            await ReloadDevicesAsync();

            _status.Text = "Urzadzenie usuniete.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
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

    private void ShowError(string message)
    {
        _status.Text = $"Blad: {message}";

        MessageBox.ErrorQuery(
            _app,
            "Blad",
            message,
            "OK"
        );
    }
}
