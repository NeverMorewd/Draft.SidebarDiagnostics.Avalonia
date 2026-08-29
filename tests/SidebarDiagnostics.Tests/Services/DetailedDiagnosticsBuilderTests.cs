using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class DetailedDiagnosticsBuilderTests
{
    [Fact]
    public void BuildIncludesCompleteCpuAndMemoryMetrics()
    {
        var snapshot = new SystemMetricsSnapshot(
            DateTimeOffset.UtcNow,
            "Test",
            25,
            6L * 1024 * 1024 * 1024,
            8L * 1024 * 1024 * 1024,
            75,
            50,
            150L * 1024 * 1024 * 1024,
            200L * 1024 * 1024 * 1024,
            1024,
            512,
            1);

        var sections = DetailedDiagnosticsBuilder.Build(snapshot, [], false);
        var cpu = Assert.Single(sections, section => section.Id == "cpu");
        var memory = Assert.Single(sections, section => section.Id == "memory");

        Assert.Contains(cpu.Metrics, metric => metric.Label == "Load" && metric.Value == "25.0%");
        Assert.Contains(cpu.Metrics, metric => metric.Label == "Model" && metric.Value == "Unknown processor");
        Assert.Contains(memory.Metrics, metric => metric.Label == "Used" && metric.Value == "6.0 GB");
        Assert.Contains(memory.Metrics, metric => metric.Label == "Free" && metric.Value == "2.0 GB");
        Assert.Contains(memory.Metrics, metric => metric.Label == "Total" && metric.Value == "8.0 GB");
    }

    [Fact]
    public void BuildIncludesNormalizedGpuMemoryCapacity()
    {
        HardwareSensorReading[] readings =
        [
            Reading("gpu-a", "GPU A", "GPU Memory Used", HardwareSensorType.Data, 2, " GB"),
            Reading("gpu-a", "GPU A", "GPU Memory Total", HardwareSensorType.Data, 8, " GB")
        ];

        var gpu = Assert.Single(DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, readings, false),
            section => section.Id == "gpu-a");

        Assert.Contains(gpu.Metrics, metric => metric.Label == "VRAM used" && metric.Value == "2.0 GB");
        Assert.Contains(gpu.Metrics, metric => metric.Label == "VRAM free" && metric.Value == "6.0 GB");
        Assert.Contains(gpu.Metrics, metric => metric.Label == "VRAM total" && metric.Value == "8.0 GB");
    }

    [Fact]
    public void BuildGroupsEveryGpuSensorByDevice()
    {
        HardwareSensorReading[] readings =
        [
            Reading("gpu-a", "GPU A", "Core Clock", HardwareSensorType.Clock, 1800, " MHz"),
            Reading("gpu-a", "GPU A", "Core Load", HardwareSensorType.Load, 55, "%"),
            Reading("gpu-b", "GPU B", "Temperature", HardwareSensorType.Temperature, 60, "°C")
        ];

        var gpuSections = DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, readings, false)
            .Where(section => section.Title == "GPU")
            .ToArray();

        Assert.Equal(2, gpuSections.Length);
        Assert.Contains(gpuSections.Single(section => section.Id == "gpu-a").Metrics, metric => metric.Label == "Model" && metric.Value == "GPU A");
        Assert.Contains(gpuSections.Single(section => section.Id == "gpu-b").Metrics, metric => metric.Label == "Model" && metric.Value == "GPU B");
    }

    [Fact]
    public void BuildKeepsEveryCoreInNaturalOrder()
    {
        var readings = Enumerable.Range(1, 16)
            .Reverse()
            .Select(core => Reading("cpu", "Test CPU", $"Core {core} Load", HardwareSensorType.Load, core, "%", HardwareDeviceType.Cpu))
            .ToArray();

        var cpu = Assert.Single(DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, readings, false), section => section.Id == "cpu");
        var cores = cpu.Metrics.Where(metric => metric.Label.StartsWith("Core ", StringComparison.Ordinal)).ToArray();

        Assert.Equal(16, cores.Length);
        Assert.Equal("Core 1 Load", cores[0].Label);
        Assert.Equal("Core 16 Load", cores[^1].Label);
    }

    [Fact]
    public void BuildSeparatesPhysicalStorageModelFromLogicalVolumes()
    {
        var reading = Reading("disk-1", "Samsung SSD 990 PRO 2TB", "Temperature", HardwareSensorType.Temperature, 42, "°C", HardwareDeviceType.Storage);

        var storage = Assert.Single(DetailedDiagnosticsBuilder.Build(SystemMetricsSnapshot.Empty, [reading], false), section => section.Id == "storage:disk-1");

        Assert.Contains(storage.Metrics, metric => metric.Label == "Model" && metric.Value == "Samsung SSD 990 PRO 2TB");
    }

    private static HardwareSensorReading Reading(
        string deviceId,
        string device,
        string sensor,
        HardwareSensorType type,
        double value,
        string unit,
        HardwareDeviceType deviceType = HardwareDeviceType.Gpu) => new(
            $"{deviceId}:{sensor}",
            deviceId,
            device,
            deviceType,
            HardwareVendor.Unknown,
            sensor,
            type,
            value,
            unit);
}
