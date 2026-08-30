using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed record ApplicationThemeOption(ApplicationTheme Value, string DisplayName)
{
    public static ApplicationThemeOption Sidebar { get; } = new(ApplicationTheme.Sidebar, "Sidebar");
    public static ApplicationThemeOption Pipboy { get; } = new(ApplicationTheme.Pipboy, "Pip-Boy");
    public static IReadOnlyList<ApplicationThemeOption> All { get; } = [Sidebar, Pipboy];
}
