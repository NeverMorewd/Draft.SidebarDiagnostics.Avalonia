using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public static class SensorCatalog
{
    public static IReadOnlyList<SensorCatalogEntry> Build(
        IEnumerable<HardwareSensorReading> readings,
        IEnumerable<SensorPreference> preferences)
    {
        var readingById = readings
            .GroupBy(reading => reading.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var preferenceById = preferences
            .Where(preference => !string.IsNullOrWhiteSpace(preference.SensorId))
            .GroupBy(preference => preference.SensorId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

        var entries = new List<SensorCatalogEntry>();
        var nextOrder = preferenceById.Count == 0
            ? 0
            : preferenceById.Values.Max(preference => preference.SortOrder) + 1;

        var orderedReadings = readingById.Values
            .OrderBy(reading => reading.Device, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reading => reading.Sensor, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        for (var index = 0; index < orderedReadings.Length; index++)
        {
            var reading = orderedReadings[index];
            preferenceById.TryGetValue(reading.Id, out var preference);
            entries.Add(new SensorCatalogEntry(
                reading.Id,
                reading.DeviceId,
                reading.Device,
                reading.Sensor,
                reading.Type,
                reading.Unit,
                true,
                preference?.IsVisible ?? preferenceById.Count == 0,
                preference?.IsPinned ?? false,
                preference?.SortOrder ?? nextOrder++,
                preference?.CustomName));
        }

        foreach (var preference in preferenceById.Values.Where(preference => !readingById.ContainsKey(preference.SensorId)))
        {
            entries.Add(new SensorCatalogEntry(
                preference.SensorId,
                string.Empty,
                "Unavailable device",
                preference.CustomName ?? preference.SensorId,
                HardwareSensorType.Unknown,
                string.Empty,
                false,
                preference.IsVisible,
                preference.IsPinned,
                preference.SortOrder,
                preference.CustomName));
        }

        return entries
            .OrderByDescending(entry => entry.IsPinned)
            .ThenBy(entry => entry.SortOrder)
            .ThenBy(entry => entry.Device, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Sensor, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<HardwareSensorReading> SelectVisible(
        IEnumerable<HardwareSensorReading> readings,
        IEnumerable<SensorPreference> preferences,
        int defaultLimit = int.MaxValue)
    {
        var readingList = readings.ToArray();
        var readingById = readingList.ToDictionary(reading => reading.Id, StringComparer.Ordinal);
        var preferenceList = preferences.ToArray();
        if (preferenceList.Length == 0)
        {
            return readingList
                .OrderBy(reading => reading.Device, StringComparer.OrdinalIgnoreCase)
                .ThenBy(reading => reading.Sensor, StringComparer.OrdinalIgnoreCase)
                .Take(defaultLimit)
                .ToArray();
        }

        return preferenceList
            .Where(preference => preference.IsVisible)
            .OrderByDescending(preference => preference.IsPinned)
            .ThenBy(preference => preference.SortOrder)
            .Select(preference => readingById.GetValueOrDefault(preference.SensorId))
            .Where(reading => reading is not null)
            .Cast<HardwareSensorReading>()
            .ToArray();
    }
}
