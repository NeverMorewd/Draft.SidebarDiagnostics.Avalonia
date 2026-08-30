using SidebarDiagnostics.App.Services.Platform;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class LinuxCpuUsageTrackerTests
{
    [Fact]
    public void CalculatesUsageFromSuccessiveSamples()
    {
        var tracker = new LinuxCpuUsageTracker();

        Assert.Equal(0, tracker.Update(800, 1000));
        Assert.Equal(75, tracker.Update(850, 1200));
    }

    [Fact]
    public void ResetsAfterCountersMoveBackwards()
    {
        var tracker = new LinuxCpuUsageTracker();
        _ = tracker.Update(800, 1000);

        Assert.Equal(0, tracker.Update(100, 200));
        Assert.Equal(50, tracker.Update(150, 300));
    }

    [Fact]
    public void RejectsIdleDeltaLargerThanTotalDelta()
    {
        var tracker = new LinuxCpuUsageTracker();
        _ = tracker.Update(100, 1000);

        Assert.Equal(0, tracker.Update(300, 1100));
    }
}
