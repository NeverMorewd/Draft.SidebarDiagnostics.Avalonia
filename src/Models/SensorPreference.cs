namespace SidebarDiagnostics.App.Models;

public sealed record SensorPreference
{
    public required string SensorId { get; init; }
    public string? CustomName { get; init; }
    public bool IsVisible { get; init; } = true;
    public bool IsPinned { get; init; }
    public int SortOrder { get; init; }
}
