using System;
using System.Windows;
using ServerDesk.Bootstrap;

namespace ServerDesk.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += LoadDevices;
    }

    private async void LoadDevices(object sender, RoutedEventArgs e)
    {
        Loaded -= LoadDevices;

        try
        {
            var services = new ServerDeskServices();
            var devices = await services.Devices.GetDevicesAsync();

            DeviceList.ItemsSource = devices;
            Status.Text = $"Zapisane urządzenia: {devices.Count}";
        }
        catch (Exception ex)
        {
            Status.Text = $"Nie udało się wczytać urządzeń: {ex.Message}";
        }
    }
}