namespace SidebarDiagnostics.App.Models;

public sealed record SensorCatalogEntry(
    string SensorId,
    string DeviceId,
    string Device,
    string Sensor,
    HardwareSensorType Type,
    string Unit,
    bool IsAvailable,
    bool IsVisible,
    bool IsPinned,
    int SortOrder,
    string? CustomName)
{
    public string DisplayName => string.IsNullOrWhiteSpace(CustomName) ? Sensor : CustomName.Trim();
}
