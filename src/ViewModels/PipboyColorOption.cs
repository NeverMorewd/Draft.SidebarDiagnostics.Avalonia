using Avalonia.Media;

namespace SidebarDiagnostics.App.ViewModels;

public sealed record PipboyColorOption(string DisplayName, string HexColor)
{
    public IBrush PreviewBrush { get; } = new SolidColorBrush(Color.Parse(HexColor));

    public static IReadOnlyList<PipboyColorOption> All { get; } =
    [
        new("Pip-Boy Green", "#15FF52"),
        new("Amber", "#FFA500"),
        new("Ice Blue", "#00BFFF"),
        new("Cyan", "#00FFEE"),
        new("Red", "#FF3030"),
        new("Purple", "#BB44FF"),
        new("Gold", "#FFD700"),
        new("Rose", "#FF4080")
    ];
}
