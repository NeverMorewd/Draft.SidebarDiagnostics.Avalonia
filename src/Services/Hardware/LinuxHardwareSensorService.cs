using System.Globalization;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class LinuxHardwareSensorService : IHardwareSensorService
{
    private const string HwmonRoot = "/sys/class/hwmon";
    private readonly Dictionary<int, (ulong Idle, ulong Total)> _previousCpuTimes = [];

    public bool IsSupported => File.Exists("/proc/stat");
    public string CapabilityMessage => "CPU details provided by procfs; hardware sensors provided by Linux hwmon when available.";

    public async ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        var readings = new List<HardwareSensorReading>();
        await ReadCpuAsync(readings, cancellationToken);
        readings.AddRange(await LinuxHwmonReader.ReadAsync(HwmonRoot, cancellationToken));

        return readings;
    }

    private async Task ReadCpuAsync(List<HardwareSensorReading> readings, CancellationToken cancellationToken)
    {
        var statLines = await File.ReadAllLinesAsync("/proc/stat", cancellationToken);
        var cpuInfo = await File.ReadAllLinesAsync("/proc/cpuinfo", cancellationToken);
        var model = cpuInfo
            .Select(line => line.Split(':', 2))
            .FirstOrDefault(parts => parts.Length == 2 && parts[0].Trim() == "model name")?[1].Trim()
            ?? "CPU";
        var frequencies = cpuInfo
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2 && parts[0].Trim() == "cpu MHz")
            .Select(parts => double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : (double?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .ToArray();

        var cpuLines = statLines.Where(line => line.StartsWith("cpu", StringComparison.Ordinal) && line.Length > 3 && char.IsDigit(line[3])).ToArray();
        for (var index = 0; index < cpuLines.Length; index++)
        {
            var values = cpuLines[index].Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)
                .Select(value => ulong.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            if (values.Length < 4) continue;
            var idle = values[3] + (values.Length > 4 ? values[4] : 0);
            var total = values.Aggregate(0UL, (sum, value) => sum + value);
            if (_previousCpuTimes.TryGetValue(index, out var previous) && total > previous.Total)
            {
                var load = (total - previous.Total - (idle - previous.Idle)) * 100d / (total - previous.Total);
                readings.Add(CpuReading(model, $"Core {index + 1} Load", index, HardwareSensorType.Load, load, "%"));
            }
            _previousCpuTimes[index] = (idle, total);
            if (index < frequencies.Length)
            {
                readings.Add(CpuReading(model, $"Core {index + 1} Clock", index, HardwareSensorType.Clock, frequencies[index], " MHz"));
            }
        }
    }

    private static HardwareSensorReading CpuReading(
        string model,
        string sensor,
        int index,
        HardwareSensorType type,
        double value,
        string unit) => new(
            $"linux:cpu:{index}:{type}",
            "linux:cpu",
            model,
            HardwareDeviceType.Cpu,
            LinuxHwmonReader.DetectVendor(model),
            sensor,
            type,
            value,
            unit);

    public void Dispose()
    {
    }
}
