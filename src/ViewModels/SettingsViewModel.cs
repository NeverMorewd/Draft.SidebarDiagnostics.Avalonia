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
    public partial double GpuAlertThreshold { get; set; }

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
    public ObservableCollection<GpuDeviceOption> GpuDevices { get; } = [];
    public ObservableCollection<ExternalMetricOptionViewModel> ExternalMetrics { get; } = [];
    public ObservableCollection<DisplayOption> Displays { get; } = [];
    public IReadOnlyList<DockEdge> DockEdges { get; } = Enum.GetValues<DockEdge>();

    [ObservableProperty]
    public partial DisplayOption? SelectedDisplay { get; set; }

    [ObservableProperty]
    public partial DockEdge DockEdge { get; set; }

    [ObservableProperty]
    public partial bool ReserveScreenSpace { get; set; }

    [ObservableProperty]
    public partial double VerticalPositionPercent { get; set; }

    [ObservableProperty]
    public partial string? ShowShortcut { get; set; }

    [ObservableProperty]
    public partial string? HideShortcut { get; set; }

    [ObservableProperty]
    public partial string? ToggleShortcut { get; set; }

    public string ShortcutStatus => _mainViewModel.ShortcutStatus;

    [ObservableProperty]
    public partial GpuDeviceOption? SelectedGpu { get; set; }

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
        GpuAlertThreshold = settings.GpuAlertThreshold;
        AlwaysOnTop = settings.AlwaysOnTop;
        LaunchAtLogin = settings.LaunchAtLogin;
        StartMinimized = settings.StartMinimized;
        ShowMachineName = settings.ShowMachineName;
        ShowClock = settings.ShowClock;
        Use24HourClock = settings.Use24HourClock;
        UseFahrenheit = settings.UseFahrenheit;
        SidebarWidth = settings.SidebarWidth;
        BackgroundOpacity = settings.BackgroundOpacity;
        DockEdge = settings.DockEdge;
        ReserveScreenSpace = settings.ReserveScreenSpace;
        VerticalPositionPercent = settings.VerticalPosition * 100;
        ShowShortcut = settings.ShowShortcut;
        HideShortcut = settings.HideShortcut;
        ToggleShortcut = settings.ToggleShortcut;

        foreach (var display in mainViewModel.AvailableDisplays)
        {
            Displays.Add(new DisplayOption(display.Id, display.Name, display.IsPrimary));
        }

        SelectedDisplay = Displays.FirstOrDefault(display => display.Id == settings.DisplayId)
            ?? Displays.FirstOrDefault(display => display.IsPrimary)
            ?? Displays.FirstOrDefault();

        foreach (var gpu in mainViewModel.LatestGpuSnapshots)
        {
            GpuDevices.Add(new GpuDeviceOption(gpu.DeviceId, gpu.Name, gpu.Vendor));
        }

        SelectedGpu = GpuDevices.FirstOrDefault(gpu => gpu.DeviceId == settings.SelectedGpuId)
            ?? GpuDevices.FirstOrDefault();

        foreach (var entry in SensorCatalog.Build(mainViewModel.LatestHardwareReadings, settings.SensorPreferences))
        {
            var option = new SensorOptionViewModel(entry);
            option.MoveRequested += MoveSensor;
            Sensors.Add(option);
        }

        foreach (var definition in settings.ExternalMetrics)
        {
            AddExternalMetric(definition);
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
            GpuAlertThreshold = GpuAlertThreshold,
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
                .ToList(),
            SelectedGpuId = SelectedGpu?.DeviceId,
            ExternalMetrics = ExternalMetrics.Select(metric => metric.ToDefinition()).ToList(),
            DisplayId = SelectedDisplay?.Id,
            DockEdge = DockEdge,
            ReserveScreenSpace = ReserveScreenSpace,
            VerticalPosition = VerticalPositionPercent / 100,
            ShowShortcut = ShowShortcut,
            HideShortcut = HideShortcut,
            ToggleShortcut = ToggleShortcut
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

    [RelayCommand]
    private void AddExternalMetric() => AddExternalMetric(new ExternalMetricDefinition());

    private void AddExternalMetric(ExternalMetricDefinition definition)
    {
        var option = new ExternalMetricOptionViewModel(definition, _mainViewModel.PreviewExternalMetricAsync);
        option.RemoveRequested += metric => ExternalMetrics.Remove(metric);
        ExternalMetrics.Add(option);
    }
}
