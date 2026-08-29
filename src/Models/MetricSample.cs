namespace SidebarDiagnostics.App.Models;

public readonly record struct MetricSample(DateTimeOffset Timestamp, double Value);
