using SidebarDiagnostics.App.Services;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class NetworkTrafficRateTrackerTests
{
    [Fact]
    public void CalculatesRatesForTheSameInterface()
    {
        var tracker = new NetworkTrafficRateTracker();
        var startedAt = DateTimeOffset.UtcNow;

        _ = tracker.Update(new NetworkTrafficSample("ethernet", 1000, 500), startedAt);
        var rate = tracker.Update(new NetworkTrafficSample("ethernet", 5000, 2500), startedAt.AddSeconds(2));

        Assert.Equal(2000, rate.DownloadBytesPerSecond);
        Assert.Equal(1000, rate.UploadBytesPerSecond);
    }

    [Fact]
    public void ResetsWhenThePrimaryInterfaceChanges()
    {
        var tracker = new NetworkTrafficRateTracker();
        var startedAt = DateTimeOffset.UtcNow;

        _ = tracker.Update(new NetworkTrafficSample("wifi", 1000, 500), startedAt);
        var changed = tracker.Update(new NetworkTrafficSample("ethernet", 9000, 7000), startedAt.AddSeconds(1));
        var next = tracker.Update(new NetworkTrafficSample("ethernet", 10000, 7500), startedAt.AddSeconds(2));

        Assert.Equal(default, changed);
        Assert.Equal(1000, next.DownloadBytesPerSecond);
        Assert.Equal(500, next.UploadBytesPerSecond);
    }

    [Fact]
    public void ResetsWhenPlatformCountersRestart()
    {
        var tracker = new NetworkTrafficRateTracker();
        var startedAt = DateTimeOffset.UtcNow;

        _ = tracker.Update(new NetworkTrafficSample("wifi", 1000, 500), startedAt);
        var restarted = tracker.Update(new NetworkTrafficSample("wifi", 10, 5), startedAt.AddSeconds(1));

        Assert.Equal(default, restarted);
    }
}
