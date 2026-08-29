using SidebarDiagnostics.App.Services.Platform;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class PlatformMetricsRuntimeTests
{
    [Fact]
    public async Task CurrentPlatformReturnsPlausibleMetrics()
    {
        var provider = PlatformMetricsProviderFactory.Create();

        _ = await provider.SampleAsync(TestContext.Current.CancellationToken);
        await Task.Delay(25, TestContext.Current.CancellationToken);
        var metrics = await provider.SampleAsync(TestContext.Current.CancellationToken);

        Assert.InRange(metrics.CpuUsagePercent, 0, 100);
        Assert.True(metrics.MemoryTotalBytes > 0);
        Assert.InRange(metrics.MemoryUsedBytes, 0, metrics.MemoryTotalBytes);
    }
}
