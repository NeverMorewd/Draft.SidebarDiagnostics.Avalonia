namespace SidebarDiagnostics.App.Models;

public sealed record ExternalMetricSnapshot(
    string Id,
    string Title,
    double? Value,
    string Unit,
    double Progress,
    string Status,
    bool IsSuccess);
