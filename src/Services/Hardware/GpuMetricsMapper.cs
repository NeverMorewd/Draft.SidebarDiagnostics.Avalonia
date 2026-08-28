using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public static class GpuMetricsMapper
{
    public static IReadOnlyList<GpuSnapshot> Map(IEnumerable<HardwareSensorReading> readings) =>
        readings
            .Where(reading => reading.DeviceType == HardwareDeviceType.Gpu)
            .GroupBy(reading => reading.DeviceId, StringComparer.Ordinal)
            .Select(MapDevice)
            .OrderBy(snapshot => snapshot.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static GpuSnapshot MapDevice(IGrouping<string, HardwareSensorReading> readings)
    {
        var values = readings.ToArray();
        var first = values[0];
        return new GpuSnapshot(
            first.DeviceId,
            first.Device,
            first.Vendor,
            Find(values, HardwareSensorType.Load, "core", "d3d 3d", "gpu"),
            Find(values, HardwareSensorType.Clock, "core"),
            Find(values, HardwareSensorType.Clock, "memory", "vram"),
            Find(values, HardwareSensorType.Temperature, "core", "gpu"),
            Find(values, HardwareSensorType.Fan, "fan"),
            Find(values, HardwareSensorType.Power, "package", "gpu"),
            FindBytes(values.Where(reading => !reading.Sensor.Contains("shared", StringComparison.OrdinalIgnoreCase)), "memory used", "vram used", "dedicated memory used"),
            FindBytes(values.Where(reading => !reading.Sensor.Contains("shared", StringComparison.OrdinalIgnoreCase)), "memory total", "vram total", "dedicated memory total"),
            FindBytes(values, "shared memory used", "gpu shared memory"));
    }

    private static double? Find(
        IEnumerable<HardwareSensorReading> readings,
        HardwareSensorType type,
        params string[] names) => readings
        .Where(reading => reading.Type == type && names.Any(name => reading.Sensor.Contains(name, StringComparison.OrdinalIgnoreCase)))
        .Select(reading => (double?)reading.Value)
        .FirstOrDefault();

    private static double? FindBytes(IEnumerable<HardwareSensorReading> readings, params string[] names)
    {
        var reading = readings.FirstOrDefault(candidate =>
            names.Any(name => candidate.Sensor.Contains(name, StringComparison.OrdinalIgnoreCase)));
        if (reading is null)
        {
            return null;
        }

        return reading.Unit.Trim() switch
        {
            "GB" => reading.Value * 1024 * 1024 * 1024,
            "MB" => reading.Value * 1024 * 1024,
            "KB" => reading.Value * 1024,
            "B" => reading.Value,
            _ => null
        };
    }
}
