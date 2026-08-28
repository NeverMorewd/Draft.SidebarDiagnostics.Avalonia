namespace SidebarDiagnostics.App.Models;

public sealed record HardwareSensorReading(
    string Device,
    string Sensor,
    double Value,
    string Unit);
