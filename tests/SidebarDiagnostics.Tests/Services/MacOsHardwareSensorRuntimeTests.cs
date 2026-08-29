using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class MacOsHardwareSensorRuntimeTests
{
    [Fact]
    public async Task ReturnsPerCoreLoadsOnMacOs()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        using var service = new MacOsHardwareSensorService();
        _ = await service.ReadAsync(TestContext.Current.CancellationToken);
        await Task.Delay(25, TestContext.Current.CancellationToken);
        var readings = await service.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Contains(readings, reading =>
            reading.DeviceType == HardwareDeviceType.Cpu
            && reading.Type == HardwareSensorType.Load
            && reading.Sensor.StartsWith("Core ", StringComparison.Ordinal));
    }
}
