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
        AddSafely(sections, () => BuildGpus(readings, gpuSnapshots ?? GpuMetricsMapper.Map(readings), fahrenheit));
        AddSafely(sections, () => BuildStorageDevices(readings, fahrenheit));
        AddSafely(sections, () => BuildDrives(readings, fahrenheit));
        AddSafely(sections, () => BuildNetworks(snapshot));
        AddSafely(sections, () => BuildHardware(readings.Where(x => x.DeviceType is HardwareDeviceType.Motherboard or HardwareDeviceType.Controller), "#F472B6", fahrenheit));
        return sections.Where(x => x.Metrics.Count > 0).ToArray();
    }

    private static void AddSafely(List<DiagnosticSection> sections, Func<IEnumerable<DiagnosticSection>> build)
    {
        try
        {
            sections.AddRange(build());
        }
        catch (Exception)
        {
        }
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
            metrics.Add(new("Model", gpu.Name));
            metrics.Add(new("Vendor", FormatVendor(gpu.Vendor)));
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
        var cpuReadings = readings.Where(reading => reading.DeviceType == HardwareDeviceType.Cpu).ToArray();
        var name = cpuReadings.FirstOrDefault()?.Device ?? "Unknown processor";
        var metrics = new List<DiagnosticMetric>
        {
            new("Model", name),
            new("Vendor", FormatVendor(cpuReadings.FirstOrDefault()?.Vendor ?? HardwareVendor.Unknown)),
            new("Architecture", System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()),
            new("Load", $"{snapshot.CpuUsagePercent:F1}%"),
            new("Logical processors", Environment.ProcessorCount.ToString(System.Globalization.CultureInfo.InvariantCulture))
        };
        metrics.AddRange(MapSensors(cpuReadings, fahrenheit));
        return new("cpu", "CPU", name, "#7DD3FC", Deduplicate(metrics));
    }

    private static DiagnosticSection BuildMemory(SystemMetricsSnapshot snapshot, IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit)
    {
        var memoryReadings = readings.Where(reading => reading.DeviceType == HardwareDeviceType.Memory).ToArray();
        var model = memoryReadings.FirstOrDefault()?.Device ?? "Physical memory";
        var metrics = new List<DiagnosticMetric>
        {
            new("Model", model),
            new("Load", $"{snapshot.MemoryUsagePercent:F1}%"),
            new("Used", FormatBytes(snapshot.MemoryUsedBytes)),
            new("Free", FormatBytes(Math.Max(0, snapshot.MemoryTotalBytes - snapshot.MemoryUsedBytes))),
            new("Total", FormatBytes(snapshot.MemoryTotalBytes))
        };
        metrics.AddRange(MapSensors(memoryReadings, fahrenheit));
        return new("memory", "RAM", model, "#C4B5FD", Deduplicate(metrics));
    }

    private static IEnumerable<DiagnosticSection> BuildDrives(IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit)
    {
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady))
        {
            var used = drive.TotalSize - drive.AvailableFreeSpace;
            var load = drive.TotalSize > 0 ? used * 100d / drive.TotalSize : 0;
            var metrics = new List<DiagnosticMetric>
            {
                new("Load", $"{load:F1}%"), new("Used", FormatBytes(used)), new("Free", FormatBytes(drive.AvailableFreeSpace)),
                new("Total", FormatBytes(drive.TotalSize)), new("Format", string.IsNullOrWhiteSpace(drive.DriveFormat) ? "Unknown" : drive.DriveFormat)
            };
            yield return new($"drive:{drive.Name}", "Volume", drive.Name, "#6EE7B7", Deduplicate(metrics));
        }
    }

    private static IEnumerable<DiagnosticSection> BuildStorageDevices(IReadOnlyList<HardwareSensorReading> readings, bool fahrenheit) =>
        readings.Where(reading => reading.DeviceType == HardwareDeviceType.Storage)
            .GroupBy(reading => reading.DeviceId, StringComparer.Ordinal)
            .Select(group => new DiagnosticSection(
                $"storage:{group.Key}",
                "Storage device",
                group.First().Device,
                "#6EE7B7",
                Deduplicate([new("Model", group.First().Device), .. MapSensors(group, fahrenheit)])));

    private static IEnumerable<DiagnosticSection> BuildNetworks(SystemMetricsSnapshot snapshot)
    {
        var activeNetworks = NetworkInterface.GetAllNetworkInterfaces()
            .Where(network => network.OperationalStatus == OperationalStatus.Up && network.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .ToArray();
        for (var index = 0; index < activeNetworks.Length; index++)
        {
            var network = activeNetworks[index];
            var addresses = network.GetIPProperties().UnicastAddresses
                .Where(x => x.Address.AddressFamily is AddressFamily.InterNetwork or AddressFamily.InterNetworkV6)
                .Select(x => x.Address.ToString()).ToArray();
            var metrics = new List<DiagnosticMetric>
            {
                new("Model", network.Description),
                new("Type", network.NetworkInterfaceType.ToString()),
                new("MAC address", FormatMacAddress(network.GetPhysicalAddress())),
                new("Link speed", FormatBitsPerSecond(network.Speed))
            };
            if (index == 0)
            {
                metrics.Add(new("Download", $"{FormatBytes(snapshot.DownloadBytesPerSecond)}/s"));
                metrics.Add(new("Upload", $"{FormatBytes(snapshot.UploadBytesPerSecond)}/s"));
            }
            if (addresses.Length > 0) metrics.Add(new("IP addresses", string.Join(", ", addresses)));
            yield return new($"network:{network.Id}", "Network", network.Name, "#FDE68A", metrics);
        }
    }

    private static IEnumerable<DiagnosticSection> BuildHardware(IEnumerable<HardwareSensorReading> readings, string color, bool fahrenheit) =>
        readings.GroupBy(x => x.DeviceId, StringComparer.Ordinal)
            .Select(group => new DiagnosticSection(
                group.Key,
                group.First().DeviceType == HardwareDeviceType.Motherboard ? "Motherboard" : "Controller",
                group.First().Device,
                color,
                Deduplicate([new("Model", group.First().Device), .. MapSensors(group, fahrenheit)])));

    private static IEnumerable<DiagnosticMetric> MapSensors(IEnumerable<HardwareSensorReading> readings, bool fahrenheit) =>
        readings.OrderBy(reading => reading.Type).ThenBy(reading => NaturalSensorOrder(reading.Sensor)).ThenBy(reading => reading.Sensor, StringComparer.OrdinalIgnoreCase).Select(reading => new DiagnosticMetric(reading.Sensor, FormatSensor(reading, fahrenheit)));

    private static int NaturalSensorOrder(string sensor)
    {
        var digits = new string(sensor.SkipWhile(character => !char.IsDigit(character)).TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var value) ? value : int.MaxValue;
    }

    private static string FormatVendor(HardwareVendor vendor) => vendor == HardwareVendor.Unknown ? "Unknown" : vendor.ToString();

    private static string FormatMacAddress(PhysicalAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes.Length == 0 ? "Unavailable" : string.Join(":", bytes.Select(value => value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture)));
    }

    private static string FormatBitsPerSecond(long bitsPerSecond) => bitsPerSecond <= 0 ? "Unknown" : $"{bitsPerSecond / 1_000_000d:F0} Mbps";

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
