namespace SidebarDiagnostics.App.Models;
public sealed record DiagnosticMetric(
    string Label,
    string Value,
    string? SeriesId = null,
    double? NumericValue = null,
    string Unit = "")
{
    public bool CanGraph => !string.IsNullOrWhiteSpace(SeriesId) && NumericValue is not null;
}
