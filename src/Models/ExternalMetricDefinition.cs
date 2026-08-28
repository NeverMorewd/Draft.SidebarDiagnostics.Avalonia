namespace SidebarDiagnostics.App.Models;

public sealed record ExternalMetricDefinition
{
    public int SchemaVersion { get; init; } = 1;
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; init; } = "External metric";
    public ExternalMetricSourceKind SourceKind { get; init; }
    public string Source { get; init; } = string.Empty;
    public string JsonPath { get; init; } = "value";
    public string Unit { get; init; } = string.Empty;
    public double Minimum { get; init; }
    public double Maximum { get; init; } = 100;
    public int RefreshIntervalSeconds { get; init; } = 15;
    public bool IsEnabled { get; init; } = true;
}
