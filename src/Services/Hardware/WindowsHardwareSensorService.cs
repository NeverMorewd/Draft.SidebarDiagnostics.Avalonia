using LibreHardwareMonitor.Hardware;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class WindowsHardwareSensorService : IHardwareSensorService
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMemoryEnabled = true,
        IsMotherboardEnabled = true,
        IsStorageEnabled = true
    };

    public WindowsHardwareSensorService()
    {
        _computer.Open();
    }

    public bool IsSupported => true;
    public string CapabilityMessage => "Hardware sensors provided by LibreHardwareMonitor.";

    public ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var readings = new List<HardwareSensorReading>();

        foreach (var hardware in _computer.Hardware)
        {
            ReadHardware(hardware, readings, cancellationToken);
        }

        return ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>(readings);
    }

    private static void ReadHardware(
        IHardware hardware,
        ICollection<HardwareSensorReading> readings,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        hardware.Update();

        foreach (var sensor in hardware.Sensors)
        {
            if (sensor.Value is { } value && TryMapType(sensor.SensorType, out var type, out var unit))
            {
                var deviceId = $"windows:{hardware.Identifier}";
                readings.Add(new HardwareSensorReading(
                    $"{deviceId}:{sensor.Identifier}",
                    deviceId,
                    hardware.Name,
                    sensor.Name,
                    type,
                    value,
                    unit));
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            ReadHardware(subHardware, readings, cancellationToken);
        }
    }

    private static bool TryMapType(SensorType sensorType, out HardwareSensorType type, out string unit)
    {
        (type, unit) = sensorType switch
        {
            SensorType.Temperature => (HardwareSensorType.Temperature, "°C"),
            SensorType.Clock => (HardwareSensorType.Clock, " MHz"),
            SensorType.Voltage => (HardwareSensorType.Voltage, " V"),
            SensorType.Load => (HardwareSensorType.Load, "%"),
            SensorType.Fan => (HardwareSensorType.Fan, " RPM"),
            SensorType.Power => (HardwareSensorType.Power, " W"),
            SensorType.Throughput => (HardwareSensorType.Throughput, " B/s"),
            _ => (HardwareSensorType.Unknown, string.Empty)
        };

        return unit.Length > 0;
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
