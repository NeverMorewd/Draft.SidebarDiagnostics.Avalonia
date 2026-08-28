namespace SidebarDiagnostics.App.Models;

public sealed record DisplayDescriptor(
    string Id,
    string Name,
    int X,
    int Y,
    int Width,
    int Height,
    double Scaling,
    bool IsPrimary);
