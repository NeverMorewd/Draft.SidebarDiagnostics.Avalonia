namespace SidebarDiagnostics.App.Models;

public sealed class MetricSeriesCatalog
{
    private readonly Dictionary<string, MetricSeries> _series = new(StringComparer.Ordinal);

    public MetricSeries? Get(string id) => _series.GetValueOrDefault(id);

    public void Update(IReadOnlyList<DiagnosticSection> sections, DateTimeOffset timestamp)
    {
        foreach (var section in sections)
        {
            foreach (var metric in section.Metrics.Where(metric => metric.CanGraph))
            {
                var id = metric.SeriesId!;
                if (!_series.TryGetValue(id, out var series))
                {
                    series = new MetricSeries(id);
                    _series.Add(id, series);
                }

                series.Update(
                    metric.Label,
                    section.Subtitle,
                    metric.Unit,
                    section.AccentResourceKey,
                    metric.NumericValue!.Value,
                    timestamp);
            }
        }
    }
}
