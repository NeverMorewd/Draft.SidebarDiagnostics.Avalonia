using System.Globalization;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class LinuxHardwareSensorService : IHardwareSensorService
{
    private const string HwmonRoot = "/sys/class/hwmon";

    public bool IsSupported => Directory.Exists(HwmonRoot);
    public string CapabilityMessage => IsSupported
        ? "Hardware temperatures provided by Linux hwmon."
        : "Linux hwmon is not available on this system.";

    public async ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!IsSupported)
        {
            return [];
        }

        var readings = new List<HardwareSensorReading>();
        foreach (var deviceDirectory in Directory.EnumerateDirectories(HwmonRoot, "hwmon*"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var deviceName = await ReadOptionalTextAsync(Path.Combine(deviceDirectory, "name"), cancellationToken)
                ?? Path.GetFileName(deviceDirectory);

            foreach (var inputPath in Directory.EnumerateFiles(deviceDirectory, "temp*_input"))
            {
                var rawValue = await ReadOptionalTextAsync(inputPath, cancellationToken);
                if (!double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var millidegrees))
                {
                    continue;
                }

                var labelPath = inputPath.Replace("_input", "_label", StringComparison.Ordinal);
                var label = await ReadOptionalTextAsync(labelPath, cancellationToken) ?? Path.GetFileNameWithoutExtension(inputPath);
                readings.Add(new HardwareSensorReading(deviceName.Trim(), label.Trim(), millidegrees / 1000d, "°C"));
            }
        }

        return readings;
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

    public void Dispose()
    {
    }
}
