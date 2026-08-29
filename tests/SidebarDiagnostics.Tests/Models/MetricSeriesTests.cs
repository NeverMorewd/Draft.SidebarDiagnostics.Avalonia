using SidebarDiagnostics.App.Styling;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Controls;
using Xunit;

namespace SidebarDiagnostics.Tests.Models;

public sealed class MetricSeriesTests
{
    [Fact]
    public void ChartRangeKeepsZeroOnlyNonNegativeSeriesAboveZero()
    {
        var range = MetricChart.CalculateRange([new(DateTimeOffset.UtcNow, 0)]);

        Assert.Equal(0, range.Minimum);
        Assert.Equal(1, range.Maximum);
    }

    [Fact]
    public void GetSamplesReturnsOnlyRequestedDuration()
    {
        var series = new MetricSeries("cpu:load");
        var now = DateTimeOffset.UtcNow;
        series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, 10, now - TimeSpan.FromSeconds(40));
        series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, 20, now - TimeSpan.FromSeconds(20));
        series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, 30, now);

        var samples = series.GetSamples(TimeSpan.FromSeconds(30), now);

        Assert.Equal([20d, 30d], samples.Select(sample => sample.Value));
    }

    [Fact]
    public void CatalogKeepsStableSeriesAcrossSectionRefreshes()
    {
        var catalog = new MetricSeriesCatalog();
        var first = Section(12);
        catalog.Update([first], DateTimeOffset.UtcNow);
        var series = catalog.Get("cpu:load");

        catalog.Update([Section(34)], DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.NotNull(series);
        Assert.Same(series, catalog.Get("cpu:load"));
        Assert.Equal(34, series.CurrentValue);
        Assert.Equal(2, series.GetSamples(TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow.AddSeconds(1)).Count);
    }

    private static DiagnosticSection Section(double value) => new(
        "cpu",
        "CPU",
        "Processor",
        ThemeResourceKeys.CpuAccent,
        [new DiagnosticMetric("Load", $"{value}%", "cpu:load", value, "%")]);
}
