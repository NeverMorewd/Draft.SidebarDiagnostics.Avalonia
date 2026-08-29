using SidebarDiagnostics.App.Services.Platform;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class CpuTickUsageTests
{
    [Fact]
    public void CalculateReturnsBusyTickPercentage()
    {
        var usage = CpuTickUsage.Calculate(100, 50, 500, 10, 130, 70, 550, 10);

        Assert.Equal(50, usage, 6);
    }

    [Fact]
    public void CalculateReturnsZeroWhenTicksDoNotAdvance()
    {
        var usage = CpuTickUsage.Calculate(100, 50, 500, 10, 100, 50, 500, 10);

        Assert.Equal(0, usage);
    }

    [Fact]
    public void CalculateHandlesCounterRollover()
    {
        var usage = CpuTickUsage.Calculate(uint.MaxValue - 4, 10, 100, 0, 5, 10, 110, 0);

        Assert.InRange(usage, 49.9, 50.1);
    }
}
