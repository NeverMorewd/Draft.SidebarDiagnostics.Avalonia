using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;
using SidebarDiagnostics.App.Styling;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class DiagnosticAlertPolicyTests
{
    [Fact]
    public void ApplyHighlightsCpuAtConfiguredThreshold()
    {
        var section = Section("cpu", "CPU", new DiagnosticMetric("Load", "91%", "cpu:load", 91, "%"));

        var result = DiagnosticAlertPolicy.Apply([section], new AppSettings { CpuAlertThreshold = 90 }, 0);

        Assert.Equal(ThemeResourceKeys.WarningAccent, Assert.Single(result).AccentResourceKey);
    }

    [Fact]
    public void ApplyLeavesMetricBelowThresholdUnchanged()
    {
        var section = Section("memory", "RAM", new DiagnosticMetric("Load", "50%", "memory:load", 50, "%"));

        var result = DiagnosticAlertPolicy.Apply([section], new AppSettings { MemoryAlertThreshold = 80 }, 0);

        Assert.Equal(ThemeResourceKeys.MemoryAccent, Assert.Single(result).AccentResourceKey);
    }

    [Fact]
    public void ApplyHighlightsPrimaryNetworkFromAggregateActivity()
    {
        var section = Section("network:primary", "Network", new DiagnosticMetric("Download", "1 MB/s"));

        var result = DiagnosticAlertPolicy.Apply([section], new AppSettings { NetworkAlertThreshold = 75 }, 80);

        Assert.Equal(ThemeResourceKeys.WarningAccent, Assert.Single(result).AccentResourceKey);
    }

    [Fact]
    public void ApplyHighlightsGpuPercentageSensor()
    {
        var section = Section("gpu:primary", "GPU", new DiagnosticMetric("GPU Core", "95%", "gpu:core", 95, "%"));

        var result = DiagnosticAlertPolicy.Apply([section], new AppSettings { GpuAlertThreshold = 90 }, 0);

        Assert.Equal(ThemeResourceKeys.WarningAccent, Assert.Single(result).AccentResourceKey);
    }

    private static DiagnosticSection Section(string id, string title, params DiagnosticMetric[] metrics) =>
        new(id, title, "Device", ThemeResourceKeys.MemoryAccent, metrics);
}
