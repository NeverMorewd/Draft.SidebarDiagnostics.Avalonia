using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class LinuxHwmonReaderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"sidebar-hwmon-{Guid.NewGuid():N}");

    [Fact]
    public async Task MapsStandardHwmonSensorsWithKernelUnits()
    {
        var device = CreateDevice("hwmon0", "amdgpu");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(device, "temp1_input"), "62500", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "temp1_label"), "Edge", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "fan1_input"), "1450", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "in1_input"), "950", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "curr1_input"), "12000", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "power1_average"), "84500000", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "power1_input"), "90000000", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(device, "freq1_input"), "2100000000", cancellationToken);

        var readings = await LinuxHwmonReader.ReadAsync(root, cancellationToken);

        AssertReading(readings, "Edge", HardwareSensorType.Temperature, 62.5, "°C");
        AssertReading(readings, "Fan 1", HardwareSensorType.Fan, 1450, " RPM");
        AssertReading(readings, "Voltage 1", HardwareSensorType.Voltage, 0.95, " V");
        AssertReading(readings, "Current 1", HardwareSensorType.Current, 12, " A");
        AssertReading(readings, "Power 1", HardwareSensorType.Power, 84.5, " W");
        AssertReading(readings, "Clock 1", HardwareSensorType.Clock, 2100, " MHz");
        Assert.All(readings, reading => Assert.Equal(HardwareDeviceType.Gpu, reading.DeviceType));
        Assert.All(readings, reading => Assert.Equal(HardwareVendor.Amd, reading.Vendor));
    }

    [Fact]
    public async Task KeepsSameNamedDevicesDistinct()
    {
        var first = CreateDevice("hwmon0", "nvidia");
        var second = CreateDevice("hwmon1", "nvidia");
        var cancellationToken = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(first, "temp1_input"), "50000", cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(second, "temp1_input"), "60000", cancellationToken);

        var readings = await LinuxHwmonReader.ReadAsync(root, cancellationToken);

        Assert.Equal(2, readings.Select(reading => reading.DeviceId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, readings.Select(reading => reading.Id).Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData("Intel(R) Core(TM) Ultra", HardwareVendor.Intel)]
    [InlineData("AMD Ryzen 9", HardwareVendor.Amd)]
    [InlineData("Apple M4", HardwareVendor.Apple)]
    public void DetectsCpuVendor(string model, HardwareVendor expected)
    {
        Assert.Equal(expected, LinuxHwmonReader.DetectVendor(model));
    }

    private string CreateDevice(string directoryName, string deviceName)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, directoryName)).FullName;
        File.WriteAllText(Path.Combine(directory, "name"), deviceName);
        return directory;
    }

    private static void AssertReading(
        IReadOnlyList<HardwareSensorReading> readings,
        string name,
        HardwareSensorType type,
        double value,
        string unit)
    {
        var reading = Assert.Single(readings, item => item.Sensor == name);
        Assert.Equal(type, reading.Type);
        Assert.Equal(value, reading.Value, 6);
        Assert.Equal(unit, reading.Unit);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
