using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services;
using SidebarDiagnostics.App.Services.Hardware;
using SidebarDiagnostics.App.Services.Startup;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemMetricsService _metricsService;
    private readonly ISettingsStore _settingsStore;
    private readonly IHardwareSensorService _hardwareSensorService;
    private readonly IAutoStartService _autoStartService;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _isDisposed;

    [ObservableProperty]
    public partial string MachineName { get; set; } = Environment.MachineName;

    [ObservableProperty]
    public partial string PlatformName { get; set; } = SystemMetricsSnapshot.Empty.Platform;

    [ObservableProperty]
    public partial string LastUpdated { get; set; } = "Starting";

    [ObservableProperty]
    public partial string ClockText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsMachineNameVisible { get; set; } = true;

    [ObservableProperty]
    public partial bool IsClockVisible { get; set; } = true;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Collecting system data";

    [ObservableProperty]
    public partial string HardwareStatus { get; set; } = "Detecting hardware sensors";

    public ObservableCollection<MetricCardViewModel> Metrics { get; } =
    [
        new("CPU", "0%", "SYSTEM LOAD", "#7DD3FC", AppSettings.Default.CpuAlertThreshold),
        new("Memory", "0 MB", "SYSTEM USED", "#C4B5FD", AppSettings.Default.MemoryAlertThreshold),
        new("Storage", "0%", "PRIMARY VOLUME", "#6EE7B7", AppSettings.Default.StorageAlertThreshold),
        new("Network", "0 B/s", "DOWNLOAD", "#FDE68A", AppSettings.Default.NetworkAlertThreshold)
    ];

    public ObservableCollection<HardwareSensorViewModel> HardwareSensors { get; } = [];

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public event EventHandler? SettingsApplied;

    public MainViewModel()
        : this(
            new SystemMetricsService(),
            new JsonSettingsStore(),
            HardwareSensorServiceFactory.Create(),
            AutoStartServiceFactory.Create())
    {
    }

    public MainViewModel(
        ISystemMetricsService metricsService,
        ISettingsStore settingsStore,
        IHardwareSensorService hardwareSensorService,
        IAutoStartService autoStartService)
    {
        _metricsService = metricsService;
        _settingsStore = settingsStore;
        _hardwareSensorService = hardwareSensorService;
        _autoStartService = autoStartService;
        _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, OnTick);
        _timer.Start();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        Settings = await _settingsStore.LoadAsync(_lifetimeCancellation.Token);
        ApplySettings(Settings);
        SettingsApplied?.Invoke(this, EventArgs.Empty);
        await RefreshAsync();
    }

    public async ValueTask SaveSettingsAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        Settings = settings.Normalize();
        ApplySettings(Settings);
        await _autoStartService.SetEnabledAsync(Settings.LaunchAtLogin, cancellationToken);
        await _settingsStore.SaveAsync(Settings, cancellationToken);
        SettingsApplied?.Invoke(this, EventArgs.Empty);
    }

    private void ApplySettings(AppSettings settings)
    {
        _timer.Interval = TimeSpan.FromMilliseconds(settings.RefreshIntervalMilliseconds);
        Metrics[0].AlertThreshold = settings.CpuAlertThreshold;
        Metrics[1].AlertThreshold = settings.MemoryAlertThreshold;
        Metrics[2].AlertThreshold = settings.StorageAlertThreshold;
        Metrics[3].AlertThreshold = settings.NetworkAlertThreshold;
        IsMachineNameVisible = settings.ShowMachineName;
        IsClockVisible = settings.ShowClock;
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        if (!await _refreshGate.WaitAsync(0, _lifetimeCancellation.Token))
        {
            return;
        }

        try
        {
            var snapshot = await _metricsService.GetSnapshotAsync(_lifetimeCancellation.Token);
            var hardwareReadings = await _hardwareSensorService.ReadAsync(_lifetimeCancellation.Token);
            PlatformName = snapshot.Platform;
            LastUpdated = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            ClockText = snapshot.Timestamp.ToLocalTime().ToString(
                Settings.Use24HourClock ? "HH:mm\ndddd, MMMM d" : "h:mm tt\ndddd, MMMM d",
                CultureInfo.CurrentCulture);
            StatusText = "Live monitoring";

            Metrics[0].Update($"{snapshot.CpuUsagePercent:F0}%", "SYSTEM LOAD", snapshot.CpuUsagePercent);
            Metrics[1].Update(FormatBytes(snapshot.MemoryUsedBytes), "SYSTEM USED", snapshot.MemoryUsagePercent);
            Metrics[2].Update($"{snapshot.StorageUsagePercent:F0}%", "PRIMARY VOLUME", snapshot.StorageUsagePercent);
            Metrics[3].Update($"{FormatBytes(snapshot.DownloadBytesPerSecond)}/s", $"UP {FormatBytes(snapshot.UploadBytesPerSecond)}/s", snapshot.NetworkActivityPercent);

            HardwareSensors.Clear();
            foreach (var reading in hardwareReadings
                         .OrderBy(reading => reading.Device, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(reading => reading.Sensor, StringComparer.OrdinalIgnoreCase)
                         .Take(12))
            {
                HardwareSensors.Add(new HardwareSensorViewModel(
                    reading.Device,
                    reading.Sensor,
                    FormatSensorValue(reading)));
            }

            HardwareStatus = HardwareSensors.Count > 0
                ? $"{HardwareSensors.Count} hardware sensors"
                : _hardwareSensorService.CapabilityMessage;
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception)
        {
            StatusText = "Metrics temporarily unavailable";
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = Math.Max(0, bytes);
        var unit = 0;

        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:F1} {units[unit]}";
    }

    private string FormatSensorValue(HardwareSensorReading reading)
    {
        if (Settings.UseFahrenheit && reading.Unit == "°C")
        {
            return $"{(reading.Value * 9 / 5) + 32:F1}°F";
        }

        return $"{reading.Value:F1}{reading.Unit}";
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _timer.Stop();
        _lifetimeCancellation.Cancel();
        _metricsService.Dispose();
        _hardwareSensorService.Dispose();
    }
}
