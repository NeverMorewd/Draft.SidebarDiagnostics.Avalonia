using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Windowing;

public static class DisplayPlacementPolicy
{
    public static WindowPlacement? Calculate(
        IReadOnlyList<DisplayDescriptor> displays,
        string? selectedDisplayId,
        DockEdge dockEdge,
        double verticalPosition,
        double logicalWindowWidth,
        double logicalWindowHeight)
    {
        if (dockEdge == DockEdge.None || displays.Count == 0)
        {
            return null;
        }

        var display = displays.FirstOrDefault(candidate => candidate.Id == selectedDisplayId)
            ?? displays.FirstOrDefault(candidate => candidate.IsPrimary)
            ?? displays[0];
        var scale = Math.Max(display.Scaling, 0.1);
        var width = (int)Math.Ceiling(logicalWindowWidth * scale);
        var maximumLogicalHeight = display.Height / scale;
        var height = Math.Min(logicalWindowHeight, maximumLogicalHeight);
        var heightPixels = (int)Math.Ceiling(height * scale);
        var availableY = Math.Max(0, display.Height - heightPixels);
        var y = display.Y + (int)Math.Round(availableY * Math.Clamp(verticalPosition, 0, 1));
        var x = dockEdge == DockEdge.Left
            ? display.X
            : display.X + display.Width - width;

        return new WindowPlacement(x, y, height);
    }
}
