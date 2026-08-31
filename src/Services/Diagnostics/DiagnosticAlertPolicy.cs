using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Styling;

namespace SidebarDiagnostics.App.Services.Diagnostics;

public static class DiagnosticAlertPolicy
{
    public static IReadOnlyList<DiagnosticSection> Apply(
        IReadOnlyList<DiagnosticSection> sections,
        AppSettings settings,
        double networkActivityPercent)
    {
        return sections.Select(section => IsAlert(section, settings, networkActivityPercent)
            ? section with { AccentResourceKey = ThemeResourceKeys.WarningAccent }
            : section).ToArray();
    }

    private static bool IsAlert(DiagnosticSection section, AppSettings settings, double networkActivityPercent)
    {
        if (section.Id == "cpu")
        {
            return ReachesThreshold(section, "Load", settings.CpuAlertThreshold);
        }

        if (section.Id == "memory")
        {
            return ReachesThreshold(section, "Load", settings.MemoryAlertThreshold);
        }

        if (section.Id.StartsWith("drive:", StringComparison.Ordinal)
            || section.Id.StartsWith("storage:", StringComparison.Ordinal))
        {
            return ReachesThreshold(section, "Load", settings.StorageAlertThreshold);
        }

        if (section.Id.StartsWith("network:", StringComparison.Ordinal))
        {
            return networkActivityPercent >= settings.NetworkAlertThreshold;
        }

        return section.Title == "GPU"
            && section.Metrics.Any(metric => metric.Unit == "%"
                && metric.NumericValue >= settings.GpuAlertThreshold);
    }

    private static bool ReachesThreshold(DiagnosticSection section, string label, double threshold) =>
        section.Metrics.Any(metric => string.Equals(metric.Label, label, StringComparison.OrdinalIgnoreCase)
            && metric.NumericValue >= threshold);
}
