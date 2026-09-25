using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;
using SidebarDiagnostics.App.ViewModels;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class DiagnosticPresentationPolicyTests
{
    [Fact]
    public void PreferencesReachTheSidebarWithoutChangingMetricIdentity()
    {
        var readings = new[] { Reading("first", "Core 1 Load"), Reading("second", "Core 2 Load") };
        var sections = DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, readings, false);
        var settings = AppSettings.Default with
        {
            SensorPreferences =
            [
                new() { SensorId = "first", IsVisible = true, SortOrder = 0 },
                new() { SensorId = "second", IsVisible = true, IsPinned = true, SortOrder = 1, CustomName = "Favorite core" }
            ]
        };
        var collection = new DiagnosticSectionCollection();
        collection.Update(sections);
        var cpu = collection.Items.Single(section => section.Id == "cpu");
        var original = cpu.Metrics.Single(metric => metric.SeriesId == "second");

        collection.Update(DiagnosticPresentationPolicy.Apply(sections, settings));

        Assert.Same(original, cpu.Metrics[0]);
        Assert.Equal("Favorite core", original.Label);
        Assert.Equal("second", original.SeriesId);
        Assert.Equal(42, original.NumericValue);
    }

    [Fact]
    public void SameNameSensorsAreNotLostInBuilderOrViewModel()
    {
        var readings = new[] { Reading("first", "Load"), Reading("second", "Load") };
        var collection = new DiagnosticSectionCollection();
        collection.Update(DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, readings, false));
        var metrics = collection.Items.Single(section => section.Id == "cpu").Metrics;

        Assert.Contains(metrics, metric => metric.SeriesId == "first");
        Assert.Contains(metrics, metric => metric.SeriesId == "second");
        Assert.Contains(metrics, metric => metric.SeriesId == "cpu:load");
    }

    [Fact]
    public void HidingEverySensorPreservesDeviceMetadataAndSummary()
    {
        var sections = DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, [Reading("first", "Core 1 Load")], false);
        var settings = AppSettings.Default with
        {
            SensorPreferences = [new() { SensorId = "first", IsVisible = false }]
        };

        var cpu = DiagnosticPresentationPolicy.Apply(sections, settings).Single(section => section.Id == "cpu");

        Assert.DoesNotContain(cpu.Metrics, metric => metric.SeriesId == "first");
        Assert.Contains(cpu.Metrics, metric => metric.Label == "Model" && metric.Value == "Test CPU");
        Assert.Contains(cpu.Metrics, metric => metric.SeriesId == "cpu:load");
    }

    [Fact]
    public void CustomOrderOverridesNaturalOrderWithinTheSection()
    {
        var sections = DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty,
            [Reading("first", "Core 1 Load"), Reading("second", "Core 2 Load")], false);
        var settings = AppSettings.Default with
        {
            SensorPreferences =
            [
                new() { SensorId = "first", IsVisible = true, SortOrder = 1 },
                new() { SensorId = "second", IsVisible = true, SortOrder = 0 }
            ]
        };
        var cpu = DiagnosticPresentationPolicy.Apply(sections, settings).Single(section => section.Id == "cpu");
        Assert.Equal(["second", "first"], cpu.Metrics.Where(metric => metric.StableId is not null).Select(metric => metric.Id));
    }

    private static HardwareSensorReading Reading(string id, string name) =>
        new(id, "cpu", "Test CPU", HardwareDeviceType.Cpu, HardwareVendor.Intel, name, HardwareSensorType.Load, 42, "%");
}
