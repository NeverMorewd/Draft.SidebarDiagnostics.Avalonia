using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed record ClockDateFormatOption(ClockDateFormat Value, string DisplayName)
{
    public static IReadOnlyList<ClockDateFormatOption> All { get; } =
    [
        new(ClockDateFormat.None, "Time only"),
        new(ClockDateFormat.MonthDay, "Month and day"),
        new(ClockDateFormat.ShortDate, "Short date"),
        new(ClockDateFormat.LongDate, "Long date")
    ];
}
