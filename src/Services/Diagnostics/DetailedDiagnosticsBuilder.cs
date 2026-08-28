using System.Net.NetworkInformation;
using System.Net.Sockets;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;

namespace SidebarDiagnostics.App.Services.Diagnostics;

public static class DetailedDiagnosticsBuilder
{
    public static IReadOnlyList<DiagnosticSection> Build(
        SystemMetricsSnapshot snapshot,
        IReadOnlyList<HardwareSensorReading> readings,
        bool fahrenheit,
        IReadOnlyList<GpuSnapshot>? gpuSnapshots = null)
    {
        var sections = new List<DiagnosticSection> { BuildCpu(snapshot, readings, fahrenheit), BuildMemory(snapshot, readings, fahrenheit) };
        sections.AddRange(BuildGpus(readings, gpuSnapshots ?? GpuMetricsMapper.Map(readings), fahrenheit));
        sections.AddRange(BuildDrives(readings, fahrenheit));
        sections.Add(BuildNetwork(snapshot));
        sections.AddRange(BuildHardware(readings.Where(x => x.DeviceType is HardwareDeviceType.Motherboard or HardwareDeviceType.Controller), "#F472B6", fahrenheit));
        return sections.Where(x => x.Metrics.Count > 0).ToArray();
    }

    private static IEnumerable<DiagnosticSection> BuildGpus(
        IReadOnlyList<HardwareSensorReading> readings,
        IReadOnlyList<GpuSnapshot> snapshots,
        bool fahrenheit)
    {
        var readingsByDevice = readings
            .Where(reading => reading.DeviceType == HardwareDeviceType.Gpu)
            .GroupBy(reading => reading.DeviceId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.AsEnumerable(), StringComparer.Ordinal);

        foreach (var gpu in snapshots)
        {
            var metrics = new List<DiagnosticMetric>();
            if (gpu.DedicatedMemoryUsedBytes is { } used)
            {
                metrics.Add(new("VRAM used", FormatBytes(used)));
            }
            if (gpu.DedicatedMemoryTotalBytes is { } total)
            {
                metrics.Add(new("VRAM total", FormatBytes(total)));
                if (gpu.DedicatedMemoryUsedBytes is { } usedBytes)
                {
                    metrics.Add(new("VRAM free", FormatBytes(Math.Max(0, total - usedBytes))));
                }
            }
            if (gpu.SharedMemoryUsedBytes is { } shared)
            {
                metrics.Add(new("Shared memory used", FormatBytes(shared)));
            }
            if (readingsByDevice.TryGetValue(gpu.DeviceId, out var deviceReadings))
            {
                metrics.AddRange(MapSensors(deviceReadings, fahrenheit));
            }
            yield return new(gpu.DeviceId, "GPU", gpu.Name, "#FB923C", Deduplicate(metrics));
        }
    }

    private static DiagnosticSection BuildCpu(SystemMetricsSnapshot snapshot, IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit)
    {
        var metrics = new List<DiagnosticMetric>
        {
            new("Load", $"{snapshot.CpuUsagePercent:F1}%"),
            new("Logical processors", Environment.ProcessorCount.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        metrics.AddRange(MapSensors(readings.Where(x => x.DeviceType == HardwareDeviceType.Cpu), fahrenheit));
        var name = readings.FirstOrDefault(x => x.DeviceType == HardwareDeviceType.Cpu)?.Device ?? System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString();
        return new("cpu", "CPU", name, "#7DD3FC", Deduplicate(metrics));
    }

    private static DiagnosticSection BuildMemory(SystemMetricsSnapshot snapshot, IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit)
    {
        var metrics = new List<DiagnosticMetric>
        {
            new("Load", $"{snapshot.MemoryUsagePercent:F1}%"),
            new("Used", FormatBytes(snapshot.MemoryUsedBytes)),
            new("Free", FormatBytes(Math.Max(0, snapshot.MemoryTotalBytes - snapshot.MemoryUsedBytes))),
            new("Total", FormatBytes(snapshot.MemoryTotalBytes))
        };
        metrics.AddRange(MapSensors(readings.Where(x => x.DeviceType == HardwareDeviceType.Memory), fahrenheit));
        return new("memory", "RAM", "Physical memory", "#C4B5FD", Deduplicate(metrics));
    }

    private static IEnumerable<DiagnosticSection> BuildDrives(IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit)
    {
        var storage = readings.Where(x => x.DeviceType == HardwareDeviceType.Storage).ToArray();
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady))
        {
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            var load = drive.TotalSize > 0 ? used * 100d / drive.TotalSize : 0;
            var metrics = new List<DiagnosticMetric>
            {
                new("Load", $"{load:F1}%"), new("Used", FormatBytes(used)), new("Free", FormatBytes(drive.AvailableFreeSpace)),
                new("Total", FormatBytes(drive.TotalSize)), new("Format", string.IsNullOrWhiteSpace(drive.DriveFormat) ? "Unknown" : drive.DriveFormat)
            };
            if (storage.Length == 1) metrics.AddRange(MapSensors(storage, fahrenheit));
            yield return new($"drive:{drive.Name}", "Drive", drive.Name, "#6EE7B7", Deduplicate(metrics));
        }
    }

    private static DiagnosticSection BuildNetwork(SystemMetricsSnapshot snapshot)
    {
        var metrics = new List<DiagnosticMetric>
        {
            new("Download", $"{FormatBytes(snapshot.DownloadBytesPerSecond)}/s"),
            new("Upload", $"{FormatBytes(snapshot.UploadBytesPerSecond)}/s")
        };
        foreach (var network in NetworkInterface.GetAllNetworkInterfaces().Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType != NetworkInterfaceType.Loopback))
        {
            var addresses = network.GetIPProperties().UnicastAddresses
                .Where(x => x.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString()).ToArray();
            if (addresses.Length > 0) metrics.Add(new(network.Name, string.Join(", ", addresses)));
        }
        return new("network", "Network", "Active interfaces", "#FDE68A", metrics);
    }

    private static IEnumerable<DiagnosticSection> BuildHardware(IEnumerable<HardwareSensorReading> readings, string color, bool fahrenheit) =>
        readings.GroupBy(x => x.DeviceId, StringComparer.Ordinal)
            .Select(group => new DiagnosticSection(group.Key, group.First().DeviceType == HardwareDeviceType.Gpu ? "GPU" : "Hardware", group.First().Device, color, Deduplicate(MapSensors(group, fahrenheit))));

    private static IEnumerable<DiagnosticMetric> MapSensors(IEnumerable<HardwareSensorReading> readings, bool fahrenheit) =>
        readings.OrderBy(x => x.Type).ThenBy(x => x.Sensor, StringComparer.OrdinalIgnoreCase).Select(x => new DiagnosticMetric(x.Sensor, FormatSensor(x, fahrenheit)));

    private static DiagnosticMetric[] Deduplicate(IEnumerable<DiagnosticMetric> metrics) =>
        metrics.GroupBy(x => x.Label, StringComparer.OrdinalIgnoreCase).Select(x => x.First()).ToArray();

    private static string FormatSensor(HardwareSensorReading reading, bool fahrenheit) =>
        fahrenheit && reading.Type == HardwareSensorType.Temperature ? $"{(reading.Value * 9 / 5) + 32:F1}°F" : $"{reading.Value:F1}{reading.Unit}";

    private static string FormatBytes(double bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"]; var value = Math.Max(0, bytes); var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:F1} {units[unit]}";
    }
}
