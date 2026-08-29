using System.Runtime.InteropServices;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Platform;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class MacOsHardwareSensorService : IHardwareSensorService
{
    private const int ProcessorCpuLoadInfo = 2;
    private const int CpuStateCount = 4;
    private static readonly Lazy<uint> TaskSelf = new(ReadTaskSelf);
    private readonly Dictionary<int, CpuTicks> previousCpuTicks = [];

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

        ReadCoreLoads(readings, model, vendor);
        return ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>(readings);
    }

    private void ReadCoreLoads(List<HardwareSensorReading> readings, string model, HardwareVendor vendor)
    {
        if (HostProcessorInfo(MachHostSelf(), ProcessorCpuLoadInfo, out var processorCount, out var processorInfo, out var infoCount) != 0)
        {
            return;
        }

        try
        {
            if (infoCount < processorCount * CpuStateCount)
            {
                return;
            }

            for (var index = 0; index < processorCount; index++)
            {
                var offset = checked((int)index * CpuStateCount * sizeof(uint));
                var current = new CpuTicks(
                    unchecked((uint)Marshal.ReadInt32(processorInfo, offset)),
                    unchecked((uint)Marshal.ReadInt32(processorInfo, offset + sizeof(uint))),
                    unchecked((uint)Marshal.ReadInt32(processorInfo, offset + (2 * sizeof(uint)))),
                    unchecked((uint)Marshal.ReadInt32(processorInfo, offset + (3 * sizeof(uint)))));

                if (previousCpuTicks.TryGetValue(index, out var previous))
                {
                    var usage = CpuTickUsage.Calculate(
                        previous.User,
                        previous.System,
                        previous.Idle,
                        previous.Nice,
                        current.User,
                        current.System,
                        current.Idle,
                        current.Nice);
                    readings.Add(new HardwareSensorReading(
                        $"macos:cpu:{index}:load",
                        "macos:cpu",
                        model,
                        HardwareDeviceType.Cpu,
                        vendor,
                        $"Core {index + 1} Load",
                        HardwareSensorType.Load,
                        usage,
                        "%"));
                }

                previousCpuTicks[index] = current;
            }
        }
        finally
        {
            _ = VmDeallocate(TaskSelf.Value, (nuint)processorInfo, checked((nuint)infoCount * sizeof(uint)));
        }
    }

    private static uint ReadTaskSelf()
    {
        var library = NativeLibrary.Load("libSystem.B.dylib");
        try
        {
            return unchecked((uint)Marshal.ReadInt32(NativeLibrary.GetExport(library, "mach_task_self_")));
        }
        finally
        {
            NativeLibrary.Free(library);
        }
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

    [DllImport("libSystem.B.dylib", EntryPoint = "mach_host_self")]
    private static extern uint MachHostSelf();

    [DllImport("libSystem.B.dylib", EntryPoint = "host_processor_info")]
    private static extern int HostProcessorInfo(
        uint host,
        int flavor,
        out uint processorCount,
        out IntPtr processorInfo,
        out uint processorInfoCount);

    [DllImport("libSystem.B.dylib", EntryPoint = "vm_deallocate")]
    private static extern int VmDeallocate(uint targetTask, nuint address, nuint size);

    public void Dispose()
    {
    }

    private readonly record struct CpuTicks(uint User, uint System, uint Idle, uint Nice);

}
