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
        series.StartRecording();
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
        series!.StartRecording();

        catalog.Update([Section(34)], DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.Same(series, catalog.Get("cpu:load"));
        Assert.Equal(34, series.CurrentValue);
        Assert.Equal(2, series.GetSamples(TimeSpan.FromMinutes(1), DateTimeOffset.UtcNow.AddSeconds(1)).Count);
    }

    [Fact]
    public void SeriesDoesNotRetainSamplesUntilRecordingStarts()
    {
        var series = new MetricSeries("cpu:load");
        var now = DateTimeOffset.UtcNow;

        for (var index = 0; index < 100; index++)
        {
            series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, index, now.AddSeconds(index));
        }

        Assert.Empty(series.GetSamples(TimeSpan.FromMinutes(5), now.AddMinutes(2)));

        series.StartRecording();

        Assert.Single(series.GetSamples(TimeSpan.FromMinutes(5), now.AddMinutes(2)));
    }

    [Fact]
    public void StoppingRecordingReleasesAllSamples()
    {
        var series = new MetricSeries("cpu:load");
        var now = DateTimeOffset.UtcNow;
        series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, 10, now);
        series.StartRecording();
        series.Update("Load", "CPU", "%", ThemeResourceKeys.CpuAccent, 20, now.AddSeconds(1));

        series.StopRecording();

        Assert.False(series.IsRecording);
        Assert.Empty(series.GetSamples(TimeSpan.FromMinutes(5), now.AddSeconds(1)));
    }

    [Fact]
    public void CatalogRemovesInactiveSeriesUnlessItIsRecording()
    {
        var catalog = new MetricSeriesCatalog();
        catalog.Update([Section(12)], DateTimeOffset.UtcNow);
        var series = catalog.Get("cpu:load")!;
        series.StartRecording();

        catalog.Update([], DateTimeOffset.UtcNow.AddSeconds(1));

        Assert.Same(series, catalog.Get("cpu:load"));

        series.StopRecording();
        catalog.Update([], DateTimeOffset.UtcNow.AddSeconds(2));

        Assert.Null(catalog.Get("cpu:load"));
    }

    private static DiagnosticSection Section(double value) => new(
        "cpu",
        "CPU",
        "Processor",
        ThemeResourceKeys.CpuAccent,
        [new DiagnosticMetric("Load", $"{value}%", "cpu:load", value, "%")]);
}
