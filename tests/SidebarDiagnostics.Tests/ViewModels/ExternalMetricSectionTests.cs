using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.ViewModels;
using Xunit;

namespace SidebarDiagnostics.Tests.ViewModels;

public sealed class ExternalMetricSectionTests
{
    [Fact]
    public void BuildExternalMetricSectionsCreatesGraphableLiveValue()
    {
        var snapshot = new ExternalMetricSnapshot("room", "Room temperature", 21.5, "°C", 50, "Live", true);

        var section = Assert.Single(MainViewModel.BuildExternalMetricSections([snapshot]));
        var metric = Assert.Single(section.Metrics);

        Assert.Equal("Room temperature", section.Title);
        Assert.Equal("21.50°C", metric.Value);
        Assert.True(metric.CanGraph);
    }

    [Fact]
    public void BuildExternalMetricSectionsSurfacesSourceFailure()
    {
        var snapshot = new ExternalMetricSnapshot("room", "Room temperature", null, "°C", 0, "Source unavailable", false);

        var section = Assert.Single(MainViewModel.BuildExternalMetricSections([snapshot]));

        Assert.Equal("Source unavailable", section.Subtitle);
        Assert.False(Assert.Single(section.Metrics).CanGraph);
    }
}
