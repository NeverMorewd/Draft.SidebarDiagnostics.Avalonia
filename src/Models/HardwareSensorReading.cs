namespace SidebarDiagnostics.App.Models;

public sealed record HardwareSensorReading(
    string Id,
    string DeviceId,
    string Device,
    HardwareDeviceType DeviceType,
    HardwareVendor Vendor,
    string Sensor,
    HardwareSensorType Type,
    double Value,
    string Unit);
