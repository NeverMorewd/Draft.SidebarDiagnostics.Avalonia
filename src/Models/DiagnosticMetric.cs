namespace SidebarDiagnostics.App.Models;

public sealed record DiagnosticMetric(
    string Label,
    string Value,
    string? SeriesId = null,
    double? NumericValue = null,
    string Unit = "")
{
    public string? StableId { get; init; }
    public string Id => StableId ?? Label;

    public bool CanGraph => !string.IsNullOrWhiteSpace(SeriesId) && NumericValue is not null;
}
