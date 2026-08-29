namespace SidebarDiagnostics.App.Models;

public sealed record AppSettings
{
    public static AppSettings Default { get; } = new();

    public int RefreshIntervalMilliseconds { get; init; } = 1000;
    public double CpuAlertThreshold { get; init; } = 85;
    public double MemoryAlertThreshold { get; init; } = 85;
    public double StorageAlertThreshold { get; init; } = 90;
    public double NetworkAlertThreshold { get; init; } = 90;
    public double GpuAlertThreshold { get; init; } = 90;
    public bool AlwaysOnTop { get; init; } = true;
    public bool LaunchAtLogin { get; init; }
    public bool StartMinimized { get; init; }
    public bool ShowMachineName { get; init; } = true;
    public bool ShowClock { get; init; } = true;
    public bool Use24HourClock { get; init; } = true;
    public bool UseFahrenheit { get; init; }
    public int SidebarWidth { get; init; } = 360;
    public double BackgroundOpacity { get; init; } = 1;
    public List<SensorPreference> SensorPreferences { get; init; } = [];
    public string? SelectedGpuId { get; init; }
    public List<ExternalMetricDefinition> ExternalMetrics { get; init; } = [];
    public string? DisplayId { get; init; }
    public DockEdge DockEdge { get; init; } = DockEdge.Right;
    public bool ReserveScreenSpace { get; init; } = true;
    public double VerticalPosition { get; init; }
    public string? ShowShortcut { get; init; } = "Ctrl+Alt+S";
    public string? HideShortcut { get; init; } = "Ctrl+Alt+H";
    public string? ToggleShortcut { get; init; } = "Ctrl+Alt+T";

    public AppSettings Normalize() => this with
    {
        RefreshIntervalMilliseconds = Math.Clamp(RefreshIntervalMilliseconds, 250, 10000),
        CpuAlertThreshold = Math.Clamp(CpuAlertThreshold, 1, 100),
        MemoryAlertThreshold = Math.Clamp(MemoryAlertThreshold, 1, 100),
        StorageAlertThreshold = Math.Clamp(StorageAlertThreshold, 1, 100),
        NetworkAlertThreshold = Math.Clamp(NetworkAlertThreshold, 1, 100),
        GpuAlertThreshold = Math.Clamp(GpuAlertThreshold, 1, 100),
        SidebarWidth = Math.Clamp(SidebarWidth, 320, 640),
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0.35, 1),
        VerticalPosition = Math.Clamp(VerticalPosition, 0, 1),
        SensorPreferences = SensorPreferences
            .Where(preference => !string.IsNullOrWhiteSpace(preference.SensorId))
            .Select(preference => preference with { SensorId = preference.SensorId.Trim() })
            .GroupBy(preference => preference.SensorId, StringComparer.Ordinal)
            .Select(group => group.Last())
            .OrderBy(preference => preference.SortOrder)
            .Select((preference, index) => preference with
            {
                CustomName = string.IsNullOrWhiteSpace(preference.CustomName) ? null : preference.CustomName.Trim(),
                SortOrder = index
            })
            .ToList(),
        ExternalMetrics = ExternalMetrics
            .Where(definition => !string.IsNullOrWhiteSpace(definition.Id))
            .GroupBy(definition => definition.Id, StringComparer.Ordinal)
            .Select(group => group.Last() with
            {
                Id = group.Key.Trim(),
                Title = string.IsNullOrWhiteSpace(group.Last().Title) ? "External metric" : group.Last().Title.Trim(),
                Source = group.Last().Source.Trim(),
                JsonPath = group.Last().JsonPath.Trim(),
                Unit = group.Last().Unit.Trim(),
                RefreshIntervalSeconds = Math.Clamp(group.Last().RefreshIntervalSeconds, 5, 3600)
            })
            .ToList()
    };
}
