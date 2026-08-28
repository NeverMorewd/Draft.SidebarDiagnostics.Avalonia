using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class GpuMetricsMapperTests
{
    [Fact]
    public void MapKeepsMultipleGpusIndependent()
    {
        HardwareSensorReading[] readings =
        [
            GpuReading("intel", "Intel Arc", HardwareVendor.Intel, "GPU Core", HardwareSensorType.Load, 35, "%"),
            GpuReading("nvidia", "NVIDIA RTX", HardwareVendor.Nvidia, "GPU Core", HardwareSensorType.Load, 82, "%")
        ];

        var snapshots = GpuMetricsMapper.Map(readings);

        Assert.Collection(
            snapshots,
            gpu =>
            {
                Assert.Equal("intel", gpu.DeviceId);
                Assert.Equal(35, gpu.LoadPercent);
            },
            gpu =>
            {
                Assert.Equal("nvidia", gpu.DeviceId);
                Assert.Equal(82, gpu.LoadPercent);
            });
    }

    [Fact]
    public void MapSeparatesDedicatedAndSharedMemory()
    {
        HardwareSensorReading[] readings =
        [
            GpuReading("intel", "Intel Graphics", HardwareVendor.Intel, "GPU Shared Memory Used", HardwareSensorType.Data, 2, " GB"),
            GpuReading("intel", "Intel Graphics", HardwareVendor.Intel, "GPU Memory Used", HardwareSensorType.Data, 0.5, " GB"),
            GpuReading("intel", "Intel Graphics", HardwareVendor.Intel, "GPU Memory Total", HardwareSensorType.Data, 1, " GB")
        ];

        var snapshot = Assert.Single(GpuMetricsMapper.Map(readings));

        Assert.Equal(2d * 1024 * 1024 * 1024, snapshot.SharedMemoryUsedBytes);
        Assert.Equal(0.5d * 1024 * 1024 * 1024, snapshot.DedicatedMemoryUsedBytes);
        Assert.Equal(1d * 1024 * 1024 * 1024, snapshot.DedicatedMemoryTotalBytes);
    }

    [Fact]
    public void MapUsesNullForUnavailableMetrics()
    {
        var reading = GpuReading(
            "amd",
            "AMD Radeon",
            HardwareVendor.Amd,
            "GPU Temperature",
            HardwareSensorType.Temperature,
            61,
            "°C");

        var snapshot = Assert.Single(GpuMetricsMapper.Map([reading]));

        Assert.Null(snapshot.LoadPercent);
        Assert.Equal(61, snapshot.TemperatureCelsius);
        Assert.Null(snapshot.DedicatedMemoryUsedBytes);
    }

    [Fact]
    public void MapIgnoresNonGpuDevices()
    {
        var reading = new HardwareSensorReading(
            "cpu:load",
            "cpu",
            "CPU",
            HardwareDeviceType.Cpu,
            HardwareVendor.Amd,
            "Core Load",
            HardwareSensorType.Load,
            50,
            "%");

        Assert.Empty(GpuMetricsMapper.Map([reading]));
    }

    private static HardwareSensorReading GpuReading(
        string deviceId,
        string device,
        HardwareVendor vendor,
        string sensor,
        HardwareSensorType type,
        double value,
        string unit) =>
        new(
            $"{deviceId}:{sensor}",
            deviceId,
            device,
            HardwareDeviceType.Gpu,
            vendor,
            sensor,
            type,
            value,
            unit);
}
