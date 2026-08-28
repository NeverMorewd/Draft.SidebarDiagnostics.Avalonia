using SidebarDiagnostics.App.Models;
using Xunit;

namespace SidebarDiagnostics.Tests.Models;

public sealed class MetricHistoryTests
{
    [Fact]
    public void ConstructorRejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MetricHistory(1));
    }

    [Fact]
    public void AddRetainsOnlyTheNewestValues()
    {
        var history = new MetricHistory(3);

        history.Add(10);
        history.Add(20);
        history.Add(30);
        history.Add(40);

        Assert.Equal([20d, 30d, 40d], history.Values);
    }

    [Theory]
    [InlineData(double.NaN, 0)]
    [InlineData(double.PositiveInfinity, 0)]
    [InlineData(-10, 0)]
    [InlineData(140, 100)]
    public void AddNormalizesInvalidValues(double value, double expected)
    {
        var history = new MetricHistory(2);

        history.Add(value);

        Assert.Equal(expected, history.Values[0]);
    }
}
