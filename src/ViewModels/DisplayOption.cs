using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed record DisplayOption(string Id, string Name, bool IsPrimary)
{
    public string DisplayName => IsPrimary ? $"{Name} · Primary" : Name;
}
