using LibreHardwareMonitor.Hardware;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;

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
            try
            {
                ReadHardware(hardware, readings, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                SafeDiagnosticLog.Write("HardwareDevice", "DeviceFailure", exception);
            }
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
        var deviceType = MapDeviceType(hardware.HardwareType);
        var vendor = MapVendor(hardware.HardwareType, hardware.Name);

        foreach (var sensor in hardware.Sensors)
        {
            try
            {
                if (sensor.Value is { } value && TryMapType(sensor.SensorType, out var type, out var unit))
                {
                    var deviceId = $"windows:{hardware.Identifier}";
                    readings.Add(new HardwareSensorReading(
                        $"{deviceId}:{sensor.Identifier}",
                        deviceId,
                        hardware.Name,
                        deviceType,
                        vendor,
                        sensor.Name,
                        type,
                        value,
                        unit));
                }
            }
            catch (Exception exception)
            {
                SafeDiagnosticLog.Write("HardwareSensor", "SensorFailure", exception);
            }
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            try
            {
                ReadHardware(subHardware, readings, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                SafeDiagnosticLog.Write("HardwareDevice", "SubDeviceFailure", exception);
            }
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
            SensorType.Data => (HardwareSensorType.Data, " GB"),
            SensorType.SmallData => (HardwareSensorType.SmallData, " MB"),
            _ => (HardwareSensorType.Unknown, string.Empty)
        };

        return unit.Length > 0;
    }

    private static HardwareDeviceType MapDeviceType(HardwareType hardwareType) => hardwareType switch
    {
        HardwareType.Cpu => HardwareDeviceType.Cpu,
        HardwareType.GpuAmd or HardwareType.GpuIntel or HardwareType.GpuNvidia => HardwareDeviceType.Gpu,
        HardwareType.Memory => HardwareDeviceType.Memory,
        HardwareType.Motherboard => HardwareDeviceType.Motherboard,
        HardwareType.Storage => HardwareDeviceType.Storage,
        HardwareType.SuperIO or HardwareType.EmbeddedController => HardwareDeviceType.Controller,
        _ => HardwareDeviceType.Unknown
    };

    private static HardwareVendor MapVendor(HardwareType hardwareType, string name)
    {
        if (hardwareType == HardwareType.GpuAmd || name.Contains("AMD", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Amd;
        }

        if (hardwareType == HardwareType.GpuIntel || name.Contains("Intel", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Intel;
        }

        if (hardwareType == HardwareType.GpuNvidia || name.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase))
        {
            return HardwareVendor.Nvidia;
        }

        return HardwareVendor.Unknown;
    }

    public void Dispose()
    {
        _computer.Close();
    }
}
