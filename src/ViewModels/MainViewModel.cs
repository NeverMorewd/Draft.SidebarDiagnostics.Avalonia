using SidebarDiagnostics.App.Styling;
using System.Collections.ObjectModel;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services;
using SidebarDiagnostics.App.Services.ExternalMetrics;
using SidebarDiagnostics.App.Services.Diagnostics;
using SidebarDiagnostics.App.Services.Hardware;
using SidebarDiagnostics.App.Services.Startup;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemMetricsService _metricsService;
    private readonly ISettingsStore _settingsStore;
    private readonly IHardwareSensorService _hardwareSensorService;
    private readonly IAutoStartService _autoStartService;
    private readonly IExternalMetricService _externalMetricService;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _isDisposed;
    private readonly Dictionary<string, MetricCardViewModel> _externalMetricCards = new(StringComparer.Ordinal);

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
        new("CPU", "0%", "SYSTEM LOAD", ThemeResourceKeys.CpuAccent, AppSettings.Default.CpuAlertThreshold),
        new("Memory", "0 MB", "SYSTEM USED", ThemeResourceKeys.MemoryAccent, AppSettings.Default.MemoryAlertThreshold),
        new("Storage", "0%", "PRIMARY VOLUME", ThemeResourceKeys.StorageAccent, AppSettings.Default.StorageAlertThreshold),
        new("Network", "0 B/s", "DOWNLOAD", ThemeResourceKeys.NetworkAccent, AppSettings.Default.NetworkAlertThreshold),
        new("GPU", "Unavailable", "NO SUPPORTED GPU METRICS", ThemeResourceKeys.GpuAccent, AppSettings.Default.GpuAlertThreshold)
    ];

    public ObservableCollection<HardwareSensorViewModel> HardwareSensors { get; } = [];

    public IReadOnlyList<HardwareSensorReading> LatestHardwareReadings { get; private set; } = [];
    public IReadOnlyList<GpuSnapshot> LatestGpuSnapshots { get; private set; } = [];
    public IReadOnlyList<DisplayDescriptor> AvailableDisplays { get; private set; } = [];
    public MetricSeriesCatalog MetricSeries { get; } = new();

    [ObservableProperty]
    public partial IReadOnlyList<DiagnosticSection> DiagnosticSections { get; set; } = [];

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public event EventHandler? SettingsApplied;
    public string ShortcutStatus { get; private set; } = "Global shortcuts are initializing.";

    public void UpdateShortcutStatus(string status) => ShortcutStatus = status;
    public event EventHandler? DisplaysChanged;

    public MainViewModel()
        : this(
            new SystemMetricsService(),
            new JsonSettingsStore(),
            HardwareSensorServiceFactory.Create(),
            AutoStartServiceFactory.Create(),
            new ExternalMetricService())
    {
    }

    public MainViewModel(
        ISystemMetricsService metricsService,
        ISettingsStore settingsStore,
        IHardwareSensorService hardwareSensorService,
        IAutoStartService autoStartService,
        IExternalMetricService externalMetricService)
    {
        _metricsService = metricsService;
        _settingsStore = settingsStore;
        _hardwareSensorService = hardwareSensorService;
        _autoStartService = autoStartService;
        _externalMetricService = externalMetricService;
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
        Metrics[4].AlertThreshold = settings.GpuAlertThreshold;
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
            var hardwareReadings = await ReadHardwareSafelyAsync(_lifetimeCancellation.Token);
            LatestHardwareReadings = hardwareReadings;
            LatestGpuSnapshots = GpuMetricsMapper.Map(hardwareReadings);
            PlatformName = snapshot.Platform;
            LastUpdated = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            ClockText = snapshot.Timestamp.ToLocalTime().ToString(
                Settings.Use24HourClock ? "HH:mm\ndddd, MMMM d" : "h:mm tt\ndddd, MMMM d",
                CultureInfo.CurrentCulture);
            StatusText = "Live monitoring";

            Metrics[0].Update($"{snapshot.CpuUsagePercent:F0}%", "SYSTEM LOAD", snapshot.CpuUsagePercent);
            Metrics[1].Update(
                $"{snapshot.MemoryUsagePercent:F0}%",
                $"{FormatBytes(snapshot.MemoryUsedBytes)} USED · {FormatBytes(snapshot.MemoryTotalBytes)} TOTAL",
                snapshot.MemoryUsagePercent);
            Metrics[2].Update(
                $"{snapshot.StorageUsagePercent:F0}%",
                $"{FormatBytes(snapshot.StorageUsedBytes)} USED · {FormatBytes(snapshot.StorageTotalBytes)} TOTAL",
                snapshot.StorageUsagePercent);
            Metrics[3].Update($"{FormatBytes(snapshot.DownloadBytesPerSecond)}/s", $"UP {FormatBytes(snapshot.UploadBytesPerSecond)}/s", snapshot.NetworkActivityPercent);
            UpdateGpuMetric();
            await UpdateExternalMetricsAsync();

            var visibleReadings = SensorCatalog.SelectVisible(hardwareReadings, Settings.SensorPreferences).ToArray();
            HardwareStatus = _hardwareSensorService.CapabilityMessage;
            DiagnosticSections = DetailedDiagnosticsBuilder.Build(
                snapshot,
                visibleReadings,
                Settings.UseFahrenheit,
                LatestGpuSnapshots);
            MetricSeries.Update(DiagnosticSections, snapshot.Timestamp);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            StatusText = $"Metrics unavailable: {exception.GetType().Name}";
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async ValueTask<IReadOnlyList<HardwareSensorReading>> ReadHardwareSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _hardwareSensorService.ReadAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            HardwareStatus = $"Hardware sensors unavailable: {exception.GetType().Name}";
            return [];
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

    private void UpdateGpuMetric()
    {
        var gpu = LatestGpuSnapshots.FirstOrDefault(candidate => candidate.DeviceId == Settings.SelectedGpuId)
            ?? (LatestGpuSnapshots.Count > 0 ? LatestGpuSnapshots[0] : null);
        if (gpu is null)
        {
            Metrics[4].Update("Unavailable", "NO SUPPORTED GPU METRICS", 0);
            return;
        }

        var value = gpu.LoadPercent is { } load ? $"{load:F0}%" : "Available";
        var details = new List<string>();
        if (gpu.TemperatureCelsius is { } temperature)
        {
            details.Add(Settings.UseFahrenheit
                ? $"{(temperature * 9 / 5) + 32:F0}°F"
                : $"{temperature:F0}°C");
        }

        if (gpu.DedicatedMemoryUsedBytes is { } used)
        {
            details.Add(gpu.DedicatedMemoryTotalBytes is { } total
                ? $"{FormatBytes(used)} / {FormatBytes(total)} VRAM"
                : $"{FormatBytes(used)} VRAM USED");
        }
        else if (gpu.SharedMemoryUsedBytes is { } shared)
        {
            details.Add($"{FormatBytes(shared)} SHARED");
        }

        Metrics[4].Update(
            value,
            details.Count > 0 ? string.Join(" · ", details) : gpu.Name.ToUpperInvariant(),
            gpu.LoadPercent ?? 0);
    }

    public ValueTask<ExternalMetricSnapshot> PreviewExternalMetricAsync(
        ExternalMetricDefinition definition,
        CancellationToken cancellationToken) => _externalMetricService.PreviewAsync(definition, cancellationToken);

    public void UpdateDisplays(IReadOnlyList<DisplayDescriptor> displays)
    {
        AvailableDisplays = displays;
        DisplaysChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task UpdateExternalMetricsAsync()
    {
        var snapshots = await _externalMetricService.ReadAsync(Settings.ExternalMetrics, _lifetimeCancellation.Token);
        var activeIds = snapshots.Select(snapshot => snapshot.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var obsolete in _externalMetricCards.Where(pair => !activeIds.Contains(pair.Key)).ToArray())
        {
            Metrics.Remove(obsolete.Value);
            _externalMetricCards.Remove(obsolete.Key);
        }

        foreach (var snapshot in snapshots)
        {
            if (!_externalMetricCards.TryGetValue(snapshot.Id, out var card))
            {
                card = new MetricCardViewModel(snapshot.Title, "Waiting", "EXTERNAL JSON", ThemeResourceKeys.ExternalAccent, 101);
                _externalMetricCards.Add(snapshot.Id, card);
                Metrics.Add(card);
            }

            card.Title = snapshot.Title;
            card.Update(
                snapshot.Value is { } value ? $"{value:F2}{snapshot.Unit}" : "Unavailable",
                snapshot.IsSuccess ? "EXTERNAL JSON" : snapshot.Status.ToUpperInvariant(),
                snapshot.Progress);
        }
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
        _timer.Tick -= OnTick;
        _lifetimeCancellation.Cancel();
        _metricsService.Dispose();
        _hardwareSensorService.Dispose();
        _externalMetricService.Dispose();
    }
}
