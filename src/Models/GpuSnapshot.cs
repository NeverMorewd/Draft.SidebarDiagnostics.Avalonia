namespace SidebarDiagnostics.App.Models;

public sealed record GpuSnapshot(
    string DeviceId,
    string Name,
    HardwareVendor Vendor,
    double? LoadPercent,
    double? CoreClockMHz,
    double? MemoryClockMHz,
    double? TemperatureCelsius,
    double? FanRpm,
    double? PowerWatts,
    double? DedicatedMemoryUsedBytes,
    double? DedicatedMemoryTotalBytes,
    double? SharedMemoryUsedBytes);
