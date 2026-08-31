using System.Globalization;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.ViewModels;
using Xunit;

namespace SidebarDiagnostics.Tests.ViewModels;

public sealed class MainViewModelClockTests
{
    [Fact]
    public void FormatClockCanHideDate()
    {
        var timestamp = new DateTimeOffset(2026, 8, 31, 21, 5, 0, TimeSpan.Zero);
        var settings = new AppSettings { Use24HourClock = true, ClockDateFormat = ClockDateFormat.None };

        Assert.Equal("21:05", MainViewModel.FormatClock(timestamp, settings));
    }

    [Fact]
    public void FormatClockUsesSelectedCultureDatePattern()
    {
        var timestamp = new DateTimeOffset(2026, 8, 31, 21, 5, 0, TimeSpan.Zero);
        var settings = new AppSettings { Use24HourClock = false, ClockDateFormat = ClockDateFormat.ShortDate };

        var value = MainViewModel.FormatClock(timestamp, settings);

        Assert.Equal(2, value.Split('\n').Length);
        Assert.EndsWith(timestamp.ToString("d", CultureInfo.CurrentCulture), value, StringComparison.Ordinal);
    }
}
