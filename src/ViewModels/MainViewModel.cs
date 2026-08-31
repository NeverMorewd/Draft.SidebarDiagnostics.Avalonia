using SidebarDiagnostics.App.Styling;
using System.Globalization;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services;
using SidebarDiagnostics.App.Services.ExternalMetrics;
using SidebarDiagnostics.App.Services.Diagnostics;
using SidebarDiagnostics.App.Services.Hardware;
using SidebarDiagnostics.App.Services.Startup;
using SidebarDiagnostics.App.Services.Networking;
using System.Collections.ObjectModel;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISystemMetricsService _metricsService;
    private readonly ISettingsStore _settingsStore;
    private readonly IHardwareSensorService _hardwareSensorService;
    private readonly IAutoStartService _autoStartService;
    private readonly IExternalMetricService _externalMetricService;
    private readonly IExternalIpAddressService _externalIpAddressService;
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCancellation = new();
    private bool _isDisposed;
    private Task? _externalIpRefreshTask;
    private string? _externalIpAddress;
    private IReadOnlyList<DiagnosticSection> _externalMetricSections = [];
    private readonly DiagnosticSectionCollection _diagnosticSections = new();

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

    public IReadOnlyList<HardwareSensorReading> LatestHardwareReadings { get; private set; } = [];
    public IReadOnlyList<GpuSnapshot> LatestGpuSnapshots { get; private set; } = [];
    public IReadOnlyList<DisplayDescriptor> AvailableDisplays { get; private set; } = [];
    public MetricSeriesCatalog MetricSeries { get; } = new();

    public ObservableCollection<DiagnosticSectionViewModel> DiagnosticSections => _diagnosticSections.Items;

    public AppSettings Settings { get; private set; } = AppSettings.Default;

    public event EventHandler? SettingsApplied;
    public string ShortcutStatus { get; private set; } = "Global shortcuts are initializing.";

    public void UpdateShortcutStatus(string status) => ShortcutStatus = status;
    public void RefreshThemeResources() => _diagnosticSections.RefreshThemeResources();
    public event EventHandler? DisplaysChanged;

    public MainViewModel()
        : this(
            new SystemMetricsService(),
            new JsonSettingsStore(),
            HardwareSensorServiceFactory.Create(),
            AutoStartServiceFactory.Create(),
            new ExternalMetricService(),
            new ExternalIpAddressService())
    {
    }

    public MainViewModel(
        ISystemMetricsService metricsService,
        ISettingsStore settingsStore,
        IHardwareSensorService hardwareSensorService,
        IAutoStartService autoStartService,
        IExternalMetricService externalMetricService,
        IExternalIpAddressService externalIpAddressService)
    {
        _metricsService = metricsService;
        _settingsStore = settingsStore;
        _hardwareSensorService = hardwareSensorService;
        _autoStartService = autoStartService;
        _externalMetricService = externalMetricService;
        _externalIpAddressService = externalIpAddressService;
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
            var snapshotTask = _metricsService.GetSnapshotAsync(_lifetimeCancellation.Token).AsTask();
            var hardwareReadingsTask = ReadHardwareSafelyAsync(_lifetimeCancellation.Token).AsTask();
            await Task.WhenAll(snapshotTask, hardwareReadingsTask);
            var snapshot = await snapshotTask;
            var hardwareReadings = await hardwareReadingsTask;
            ScheduleExternalIpRefresh();
            LatestHardwareReadings = hardwareReadings;
            LatestGpuSnapshots = GpuMetricsMapper.Map(hardwareReadings);
            PlatformName = snapshot.Platform;
            LastUpdated = snapshot.Timestamp.ToLocalTime().ToString("HH:mm:ss", CultureInfo.CurrentCulture);
            ClockText = FormatClock(snapshot.Timestamp.ToLocalTime(), Settings);
            StatusText = "Live monitoring";

            await UpdateExternalMetricsAsync();

            var visibleReadings = SensorCatalog.SelectVisible(hardwareReadings, Settings.SensorPreferences).ToArray();
            HardwareStatus = _hardwareSensorService.CapabilityMessage;
            var sections = DetailedDiagnosticsBuilder.Build(
                snapshot,
                visibleReadings,
                Settings.UseFahrenheit,
                LatestGpuSnapshots,
                _externalIpAddress);
            var diagnosticSnapshots = DiagnosticAlertPolicy.Apply(
                [.. sections, .. _externalMetricSections],
                Settings,
                snapshot.NetworkActivityPercent);
            _diagnosticSections.Update(diagnosticSnapshots);
            MetricSeries.Update(diagnosticSnapshots, snapshot.Timestamp);
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

    private void ScheduleExternalIpRefresh()
    {
        if (!Settings.ShowExternalIpAddress)
        {
            _externalIpAddress = null;
            return;
        }

        if (_externalIpRefreshTask is null || _externalIpRefreshTask.IsCompleted)
        {
            _externalIpRefreshTask = RefreshExternalIpAddressAsync();
        }
    }

    private async Task RefreshExternalIpAddressAsync()
    {
        try
        {
            _externalIpAddress = await _externalIpAddressService.GetAddressAsync(_lifetimeCancellation.Token);
        }
        catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SafeDiagnosticLog.Write("ExternalIp", "RefreshFailure", exception);
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

    internal static string FormatClock(DateTimeOffset timestamp, AppSettings settings)
    {
        var time = timestamp.ToString(settings.Use24HourClock ? "HH:mm" : "h:mm tt", CultureInfo.CurrentCulture);
        var dateFormat = settings.ClockDateFormat switch
        {
            ClockDateFormat.None => null,
            ClockDateFormat.MonthDay => "M",
            ClockDateFormat.ShortDate => "d",
            ClockDateFormat.LongDate => "D",
            _ => "D"
        };
        return dateFormat is null
            ? time
            : $"{time}\n{timestamp.ToString(dateFormat, CultureInfo.CurrentCulture)}";
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
        _externalMetricSections = BuildExternalMetricSections(snapshots);
    }

    internal static IReadOnlyList<DiagnosticSection> BuildExternalMetricSections(
        IReadOnlyList<ExternalMetricSnapshot> snapshots) => snapshots.Select(snapshot =>
    {
        var value = snapshot.Value is { } numericValue ? $"{numericValue:F2}{snapshot.Unit}" : "Unavailable";
        var metric = snapshot.Value is { } number
            ? new DiagnosticMetric("Value", value, $"external:{snapshot.Id}:value", number, snapshot.Unit)
            : new DiagnosticMetric("Value", value);
        return new DiagnosticSection(
            $"external:{snapshot.Id}",
            snapshot.Title,
            snapshot.IsSuccess ? "External JSON metric" : snapshot.Status,
            ThemeResourceKeys.ExternalAccent,
            [metric]);
    }).ToArray();

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
        _externalIpAddressService.Dispose();
    }
}
