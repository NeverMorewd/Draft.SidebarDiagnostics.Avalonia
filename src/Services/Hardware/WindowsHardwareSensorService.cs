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
            if (sensor.Value is { } value && TryGetUnit(sensor.SensorType, out var unit))
            {
                readings.Add(new HardwareSensorReading(hardware.Name, sensor.Name, value, unit));
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            ReadHardware(subHardware, readings, cancellationToken);
        }
    }

    private static bool TryGetUnit(SensorType sensorType, out string unit)
    {
        unit = sensorType switch
        {
            SensorType.Temperature => "°C",
            SensorType.Clock => " MHz",
            SensorType.Voltage => " V",
            SensorType.Load => "%",
            SensorType.Fan => " RPM",
            SensorType.Power => " W",
            SensorType.Throughput => " B/s",
            _ => string.Empty
        };

        return unit.Length > 0;
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
