using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Windowing;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class DisplayPlacementPolicyTests
{
    [Fact]
    public void RightDockUsesPhysicalPixelsAtTargetScale()
    {
        DisplayDescriptor[] displays =
        [
            new("uhd", "UHD", 1920, 0, 3840, 2080, 1.5, false)
        ];

        var placement = DisplayPlacementPolicy.Calculate(displays, "uhd", DockEdge.Right, 0, 360, 900);

        Assert.NotNull(placement);
        Assert.Equal(5220, placement.X);
        Assert.Equal(0, placement.Y);
        Assert.Equal(900, placement.Height);
    }

    [Fact]
    public void MissingDisplayFallsBackToPrimary()
    {
        DisplayDescriptor[] displays =
        [
            new("secondary", "Secondary", -1920, 0, 1920, 1040, 1, false),
            new("primary", "Primary", 0, 0, 1920, 1040, 1, true)
        ];

        var placement = DisplayPlacementPolicy.Calculate(displays, "missing", DockEdge.Left, 0, 360, 900);

        Assert.NotNull(placement);
        Assert.Equal(0, placement.X);
    }

    [Fact]
    public void VerticalPositionUsesRemainingWorkingArea()
    {
        DisplayDescriptor[] displays =
        [
            new("display", "Display", 0, 40, 1920, 1040, 1, true)
        ];

        var placement = DisplayPlacementPolicy.Calculate(displays, "display", DockEdge.Left, 0.5, 360, 800);

        Assert.NotNull(placement);
        Assert.Equal(160, placement.Y);
    }

    [Fact]
    public void OversizedWindowIsConstrainedToWorkingArea()
    {
        DisplayDescriptor[] displays =
        [
            new("display", "Display", 0, 0, 1920, 900, 1.25, true)
        ];

        var placement = DisplayPlacementPolicy.Calculate(displays, "display", DockEdge.Right, 1, 360, 1200);

        Assert.NotNull(placement);
        Assert.Equal(720, placement.Height);
        Assert.Equal(0, placement.Y);
    }

    [Fact]
    public void FreePlacementDoesNotMoveWindow()
    {
        DisplayDescriptor[] displays =
        [
            new("display", "Display", 0, 0, 1920, 1080, 1, true)
        ];

        Assert.Null(DisplayPlacementPolicy.Calculate(displays, "display", DockEdge.None, 0, 360, 900));
    }
}
