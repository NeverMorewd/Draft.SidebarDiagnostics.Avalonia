using SidebarDiagnostics.App.Services.Platform;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class LinuxMetricsProviderTests
{
    [Fact]
    public void UsesKernelProvidedAvailableMemory()
    {
        var result = LinuxMetricsProvider.ParseMemory(
        [
            "MemTotal:       1000 kB",
            "MemFree:         100 kB",
            "MemAvailable:    400 kB",
            "Cached:          200 kB"
        ]);

        Assert.Equal(1000 * 1024, result.Total);
        Assert.Equal(600 * 1024, result.Used);
    }

    [Fact]
    public void EstimatesAvailableMemoryWhenKernelValueIsMissing()
    {
        var result = LinuxMetricsProvider.ParseMemory(
        [
            "MemTotal:       1000 kB",
            "MemFree:         100 kB",
            "Buffers:          50 kB",
            "Cached:          200 kB",
            "SReclaimable:     30 kB",
            "Shmem:            20 kB"
        ]);

        Assert.Equal(640 * 1024, result.Used);
    }

    [Fact]
    public void RejectsMissingTotalMemory()
    {
        Assert.Throws<InvalidDataException>(() => LinuxMetricsProvider.ParseMemory(["MemFree: 100 kB"]));
    }
}
