using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.ViewModels;
using Xunit;

namespace SidebarDiagnostics.Tests.ViewModels;

public sealed class DiagnosticSectionCollectionTests
{
    [Fact]
    public void UpdatePreservesSectionAndMetricInstances()
    {
        var collection = new DiagnosticSectionCollection();
        collection.Update([Section("cpu", "CPU", Metric("Load", "10%", 10))]);
        var section = collection.Items[0];
        var metric = section.Metrics[0];
        var sectionCollectionChanges = 0;
        var metricCollectionChanges = 0;
        collection.Items.CollectionChanged += (_, _) => sectionCollectionChanges++;
        section.Metrics.CollectionChanged += (_, _) => metricCollectionChanges++;

        collection.Update([Section("cpu", "Processor", Metric("Load", "25%", 25))]);

        Assert.Same(section, collection.Items[0]);
        Assert.Same(metric, collection.Items[0].Metrics[0]);
        Assert.Equal(0, sectionCollectionChanges);
        Assert.Equal(0, metricCollectionChanges);
        Assert.Equal("Processor", section.Title);
        Assert.Equal("25%", metric.Value);
        Assert.Equal(25, metric.NumericValue);
    }

    [Fact]
    public void UpdateReconcilesOrderAndRemovesMissingItems()
    {
        var collection = new DiagnosticSectionCollection();
        collection.Update([
            Section("cpu", "CPU", Metric("Load", "10%", 10), Metric("Clock", "4 GHz", 4)),
            Section("memory", "RAM", Metric("Used", "8 GB", 8))
        ]);
        var memory = collection.Items[1];
        var clock = collection.Items[0].Metrics[1];

        collection.Update([
            Section("memory", "RAM", Metric("Used", "9 GB", 9)),
            Section("cpu", "CPU", Metric("Clock", "4.1 GHz", 4.1))
        ]);

        Assert.Same(memory, collection.Items[0]);
        Assert.Equal("memory", collection.Items[0].Id);
        Assert.Equal("cpu", collection.Items[1].Id);
        Assert.Single(collection.Items[1].Metrics);
        Assert.Same(clock, collection.Items[1].Metrics[0]);
        Assert.Equal("4.1 GHz", clock.Value);
    }

    [Fact]
    public void GraphAvailabilityUpdatesWithoutReplacingMetric()
    {
        var collection = new DiagnosticSectionCollection();
        collection.Update([Section("external:one", "External", new DiagnosticMetric("Value", "Unavailable"))]);
        var metric = collection.Items[0].Metrics[0];

        collection.Update([Section("external:one", "External", Metric("Value", "42", 42))]);

        Assert.Same(metric, collection.Items[0].Metrics[0]);
        Assert.True(metric.CanGraph);
        Assert.Equal("value", metric.SeriesId);
    }

    private static DiagnosticSection Section(string id, string title, params DiagnosticMetric[] metrics) =>
        new(id, title, title, "AccentCpuBrush", metrics);

    private static DiagnosticMetric Metric(string label, string value, double numericValue) =>
        new(label, value, label.ToLowerInvariant(), numericValue, string.Empty);
}
