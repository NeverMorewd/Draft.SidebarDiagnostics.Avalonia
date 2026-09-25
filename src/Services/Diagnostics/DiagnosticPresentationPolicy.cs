using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Diagnostics;

public static class DiagnosticPresentationPolicy
{
    public static IReadOnlyList<DiagnosticSection> Apply(
        IReadOnlyList<DiagnosticSection> sections,
        AppSettings settings)
    {
        var preferences = settings.SensorPreferences.ToDictionary(item => item.SensorId, StringComparer.Ordinal);
        return sections.Select(section => section with
        {
            Metrics = section.Metrics
                .Where(metric => metric.StableId is null || preferences.Count == 0
                    || preferences.TryGetValue(metric.StableId, out var preference) && preference.IsVisible)
                .OrderByDescending(metric => Preference(metric)?.IsPinned == true)
                .ThenBy(metric => metric.StableId is null ? 0 : 1)
                .ThenBy(metric => Preference(metric)?.SortOrder ?? int.MaxValue)
                .Select(metric => Preference(metric)?.CustomName is { } name && !string.IsNullOrWhiteSpace(name)
                    ? metric with { Label = name.Trim() }
                    : metric)
                .ToArray()
        }).ToArray();

        SensorPreference? Preference(DiagnosticMetric metric) => metric.StableId is { } id
            ? preferences.GetValueOrDefault(id)
            : null;
    }
}
