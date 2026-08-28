using System.Runtime.InteropServices;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class MacOsHardwareSensorService : IHardwareSensorService
{
    public bool IsSupported => true;
    public string CapabilityMessage => "CPU details provided by macOS sysctl. Hardware temperatures are not exposed through a stable public API.";

    public ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var model = ReadString("machdep.cpu.brand_string") ?? ReadString("hw.model") ?? "Apple CPU";
        var vendor = model.Contains("Apple", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Apple
            : model.Contains("Intel", StringComparison.OrdinalIgnoreCase) ? HardwareVendor.Intel
            : HardwareVendor.Unknown;
        var readings = new List<HardwareSensorReading>();
        if (ReadUInt64("hw.cpufrequency") is { } frequency)
        {
            readings.Add(new HardwareSensorReading(
                "macos:cpu:clock",
                "macos:cpu",
                model,
                HardwareDeviceType.Cpu,
                vendor,
                "Clock",
                HardwareSensorType.Clock,
                frequency / 1_000_000d,
                " MHz"));
        }
        return ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>(readings);
    }

    private static string? ReadString(string name)
    {
        nuint length = 0;
        if (SysctlByName(name, IntPtr.Zero, ref length, IntPtr.Zero, 0) != 0 || length == 0) return null;
        var buffer = Marshal.AllocHGlobal((int)length);
        try
        {
            if (SysctlByName(name, buffer, ref length, IntPtr.Zero, 0) != 0) return null;
            return Marshal.PtrToStringUTF8(buffer)?.TrimEnd('\0');
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static ulong? ReadUInt64(string name)
    {
        nuint length = sizeof(ulong);
        var buffer = Marshal.AllocHGlobal(sizeof(ulong));
        try
        {
            if (SysctlByName(name, buffer, ref length, IntPtr.Zero, 0) != 0) return null;
            return unchecked((ulong)Marshal.ReadInt64(buffer));
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    [DllImport(
        "libSystem.B.dylib",
        EntryPoint = "sysctlbyname",
        CharSet = CharSet.Ansi,
        BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    private static extern int SysctlByName(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        IntPtr oldValue,
        ref nuint oldValueLength,
        IntPtr newValue,
        nuint newValueLength);

    public void Dispose()
    {
    }
}
