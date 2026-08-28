using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class SettingsViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty]
    public partial int RefreshIntervalMilliseconds { get; set; }

    [ObservableProperty]
    public partial double CpuAlertThreshold { get; set; }

    [ObservableProperty]
    public partial double MemoryAlertThreshold { get; set; }

    [ObservableProperty]
    public partial double StorageAlertThreshold { get; set; }

    [ObservableProperty]
    public partial double NetworkAlertThreshold { get; set; }

    [ObservableProperty]
    public partial bool AlwaysOnTop { get; set; }

    [ObservableProperty]
    public partial bool LaunchAtLogin { get; set; }

    [ObservableProperty]
    public partial bool StartMinimized { get; set; }

    [ObservableProperty]
    public partial bool ShowMachineName { get; set; }

    [ObservableProperty]
    public partial bool ShowClock { get; set; }

    [ObservableProperty]
    public partial bool Use24HourClock { get; set; }

    [ObservableProperty]
    public partial bool UseFahrenheit { get; set; }

    [ObservableProperty]
    public partial int SidebarWidth { get; set; }

    [ObservableProperty]
    public partial double BackgroundOpacity { get; set; }

    [ObservableProperty]
    public partial string SensorSearchText { get; set; } = string.Empty;

    public ObservableCollection<SensorOptionViewModel> Sensors { get; } = [];

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public SettingsViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
        var settings = mainViewModel.Settings;
        RefreshIntervalMilliseconds = settings.RefreshIntervalMilliseconds;
        CpuAlertThreshold = settings.CpuAlertThreshold;
        MemoryAlertThreshold = settings.MemoryAlertThreshold;
        StorageAlertThreshold = settings.StorageAlertThreshold;
        NetworkAlertThreshold = settings.NetworkAlertThreshold;
        AlwaysOnTop = settings.AlwaysOnTop;
        LaunchAtLogin = settings.LaunchAtLogin;
        StartMinimized = settings.StartMinimized;
        ShowMachineName = settings.ShowMachineName;
        ShowClock = settings.ShowClock;
        Use24HourClock = settings.Use24HourClock;
        UseFahrenheit = settings.UseFahrenheit;
        SidebarWidth = settings.SidebarWidth;
        BackgroundOpacity = settings.BackgroundOpacity;

        foreach (var entry in SensorCatalog.Build(mainViewModel.LatestHardwareReadings, settings.SensorPreferences))
        {
            var option = new SensorOptionViewModel(entry);
            option.MoveRequested += MoveSensor;
            Sensors.Add(option);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = new AppSettings
        {
            RefreshIntervalMilliseconds = RefreshIntervalMilliseconds,
            CpuAlertThreshold = CpuAlertThreshold,
            MemoryAlertThreshold = MemoryAlertThreshold,
            StorageAlertThreshold = StorageAlertThreshold,
            NetworkAlertThreshold = NetworkAlertThreshold,
            AlwaysOnTop = AlwaysOnTop,
            LaunchAtLogin = LaunchAtLogin,
            StartMinimized = StartMinimized,
            ShowMachineName = ShowMachineName,
            ShowClock = ShowClock,
            Use24HourClock = Use24HourClock,
            UseFahrenheit = UseFahrenheit,
            SidebarWidth = SidebarWidth,
            BackgroundOpacity = BackgroundOpacity,
            SensorPreferences = Sensors
                .Select((sensor, index) => sensor.ToPreference(index))
                .ToList()
        };

        await _mainViewModel.SaveSettingsAsync(settings, CancellationToken.None);
        Saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel() => Cancelled?.Invoke(this, EventArgs.Empty);

    partial void OnSensorSearchTextChanged(string value)
    {
        foreach (var sensor in Sensors)
        {
            sensor.UpdateSearch(value);
        }
    }

    private void MoveSensor(SensorOptionViewModel sensor, int offset)
    {
        var currentIndex = Sensors.IndexOf(sensor);
        var targetIndex = Math.Clamp(currentIndex + offset, 0, Sensors.Count - 1);
        if (currentIndex >= 0 && currentIndex != targetIndex)
        {
            Sensors.Move(currentIndex, targetIndex);
        }
    }
}
