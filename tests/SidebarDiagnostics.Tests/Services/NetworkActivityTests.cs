using SidebarDiagnostics.App.Services;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class NetworkActivityTests
{
    [Theory]
    [InlineData(12_500_000, 0, 1_000_000_000, 10)]
    [InlineData(12_500_000, 12_500_000, 100_000_000, 100)]
    [InlineData(0, 6_250_000, 100_000_000, 50)]
    [InlineData(1_000, 1_000, 0, 0)]
    [InlineData(125_000_000, 0, 100_000_000, 100)]
    public void ActivityUsesTheBusiestDirectionAndReportedLinkSpeed(double download, double upload, long speed, double expected)
    {
        Assert.Equal(expected, NetworkTrafficRateTracker.CalculateActivityPercent(new(download, upload), speed));
    }
}
