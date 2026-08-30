using System.Globalization;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

internal static class LinuxHwmonReader
{
    private static readonly HwmonSensorDefinition[] SensorDefinitions =
    [
        new("temp", HardwareSensorType.Temperature, 0.001, "°C"),
        new("fan", HardwareSensorType.Fan, 1, " RPM"),
        new("in", HardwareSensorType.Voltage, 0.001, " V"),
        new("curr", HardwareSensorType.Current, 0.001, " A"),
        new("power", HardwareSensorType.Power, 0.000001, " W", "average", "input"),
        new("freq", HardwareSensorType.Clock, 0.000001, " MHz")
    ];

    public static async ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(
        string root,
        CancellationToken cancellationToken)
    {
        var readings = new List<HardwareSensorReading>();
        if (!Directory.Exists(root))
        {
            return readings;
        }

        foreach (var deviceDirectory in Directory.EnumerateDirectories(root, "hwmon*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deviceName = (await ReadOptionalTextAsync(Path.Combine(deviceDirectory, "name"), cancellationToken)
                ?? Path.GetFileName(deviceDirectory)).Trim();
            var deviceId = CreateDeviceId(deviceDirectory, deviceName);

            foreach (var definition in SensorDefinitions)
            {
                foreach (var inputPath in EnumerateInputs(deviceDirectory, definition))
                {
                    var rawValue = await ReadOptionalTextAsync(inputPath, cancellationToken);
                    if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                    {
                        continue;
                    }

                    var basePath = GetSensorBasePath(inputPath, definition);
                    var label = (await ReadOptionalTextAsync(basePath + "_label", cancellationToken))?.Trim()
                        ?? CreateFallbackLabel(basePath, definition.Prefix);
                    readings.Add(new HardwareSensorReading(
                        $"{deviceId}:{Path.GetFileName(basePath)}",
                        deviceId,
                        deviceName,
                        DetectDeviceType(deviceName),
                        DetectVendor(deviceName),
                        label,
                        definition.Type,
                        value * definition.Scale,
                        definition.Unit));
                }
            }
        }

        return readings;
    }

    private static IEnumerable<string> EnumerateInputs(string directory, HwmonSensorDefinition definition) =>
        definition.Suffixes
            .SelectMany(suffix => Directory.EnumerateFiles(directory, $"{definition.Prefix}*_{suffix}"))
            .Where(path =>
            {
                var fileName = Path.GetFileName(path);
                var suffixLength = fileName.LastIndexOf('_');
                var index = fileName.AsSpan(definition.Prefix.Length, suffixLength - definition.Prefix.Length);
                return index.Length > 0 && index.IndexOfAnyExceptInRange('0', '9') < 0;
            })
            .GroupBy(path => GetSensorBasePath(path, definition), StringComparer.Ordinal)
            .Select(group => group.First());

    private static string GetSensorBasePath(string inputPath, HwmonSensorDefinition definition)
    {
        foreach (var suffix in definition.Suffixes)
        {
            var marker = $"_{suffix}";
            if (inputPath.EndsWith(marker, StringComparison.Ordinal))
            {
                return inputPath[..^marker.Length];
            }
        }

        return inputPath;
    }

    private static string CreateFallbackLabel(string basePath, string prefix)
    {
        var fileName = Path.GetFileName(basePath);
        var index = fileName[prefix.Length..];
        return $"{prefix switch
        {
            "temp" => "Temperature",
            "fan" => "Fan",
            "in" => "Voltage",
            "curr" => "Current",
            "power" => "Power",
            "freq" => "Clock",
            _ => "Sensor"
        }} {index}";
    }

    private static string CreateDeviceId(string directory, string deviceName)
    {
        var devicePath = Path.Combine(directory, "device");
        string identity;
        try
        {
            identity = new DirectoryInfo(devicePath).ResolveLinkTarget(true)?.FullName
                ?? Path.GetFileName(directory);
        }
        catch (IOException)
        {
            identity = Path.GetFileName(directory);
        }

        return $"linux:hwmon:{deviceName}:{identity.Replace(Path.DirectorySeparatorChar, ':')}";
    }

    internal static HardwareDeviceType DetectDeviceType(string deviceName) =>
        deviceName.Contains("amdgpu", StringComparison.OrdinalIgnoreCase)
        || deviceName.Contains("i915", StringComparison.OrdinalIgnoreCase)
        || deviceName.Contains("nouveau", StringComparison.OrdinalIgnoreCase)
        || deviceName.Contains("nvidia", StringComparison.OrdinalIgnoreCase)
            ? HardwareDeviceType.Gpu
            : HardwareDeviceType.Unknown;

    internal static HardwareVendor DetectVendor(string deviceName)
    {
        if (deviceName.Contains("amd", StringComparison.OrdinalIgnoreCase)) return HardwareVendor.Amd;
        if (deviceName.Contains("intel", StringComparison.OrdinalIgnoreCase)
            || deviceName.Contains("i915", StringComparison.OrdinalIgnoreCase)) return HardwareVendor.Intel;
        if (deviceName.Contains("nouveau", StringComparison.OrdinalIgnoreCase)
            || deviceName.Contains("nvidia", StringComparison.OrdinalIgnoreCase)) return HardwareVendor.Nvidia;
        if (deviceName.Contains("apple", StringComparison.OrdinalIgnoreCase)) return HardwareVendor.Apple;
        return HardwareVendor.Unknown;
    }

    private static async Task<string?> ReadOptionalTextAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return await File.ReadAllTextAsync(path, cancellationToken);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private sealed record HwmonSensorDefinition
    {
        public HwmonSensorDefinition(
            string prefix,
            HardwareSensorType type,
            double scale,
            string unit,
            params string[] suffixes)
        {
            Prefix = prefix;
            Type = type;
            Scale = scale;
            Unit = unit;
            Suffixes = suffixes.Length == 0 ? ["input"] : suffixes;
        }

        public string Prefix { get; }
        public HardwareSensorType Type { get; }
        public double Scale { get; }
        public string Unit { get; }
        public string[] Suffixes { get; }
    }
}
