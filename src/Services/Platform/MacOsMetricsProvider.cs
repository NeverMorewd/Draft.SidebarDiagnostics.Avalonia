using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SidebarDiagnostics.App.Services.Platform;

public sealed class MacOsMetricsProvider : IPlatformMetricsProvider
{
    private const int HostCpuLoadInfo = 3;
    private const int HostVmInfo64 = 4;
    private readonly object cpuSync = new();
    private CpuTicks? previousCpuTicks;

    public ValueTask<PlatformMetrics> SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        nuint memorySizeLength = sizeof(ulong);
        if (SysctlByName("hw.memsize", out var totalMemory, ref memorySizeLength, IntPtr.Zero, 0) != 0)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        if (HostPageSize(MachHostSelf(), out var pageSize) != 0)
        {
            throw new InvalidOperationException("macOS did not return the virtual memory page size.");
        }

        var statistics = new VmStatistics64();
        var count = (uint)(Marshal.SizeOf<VmStatistics64>() / sizeof(int));
        if (HostStatistics64(MachHostSelf(), HostVmInfo64, ref statistics, ref count) != 0)
        {
            throw new InvalidOperationException("macOS did not return virtual memory statistics.");
        }

        var usedPages = (ulong)statistics.ActiveCount
            + statistics.InactiveCount
            + statistics.WireCount
            + statistics.CompressorPageCount;
        var usedMemory = checked((long)(usedPages * pageSize));
        var cpuUsage = ReadCpuUsage(MachHostSelf());

        return ValueTask.FromResult(new PlatformMetrics(
            Math.Clamp(cpuUsage, 0, 100),
            usedMemory,
            checked((long)totalMemory)));
    }

    private double ReadCpuUsage(uint host)
    {
        var load = new HostCpuLoadStatistics();
        var count = (uint)(Marshal.SizeOf<HostCpuLoadStatistics>() / sizeof(int));
        if (HostStatistics(host, HostCpuLoadInfo, ref load, ref count) != 0)
        {
            throw new InvalidOperationException("macOS did not return CPU load statistics.");
        }

        var current = new CpuTicks(load.User, load.System, load.Idle, load.Nice);
        lock (cpuSync)
        {
            var previous = previousCpuTicks;
            previousCpuTicks = current;
            if (previous is null)
            {
                return 0;
            }

            return CpuTickUsage.Calculate(
                previous.Value.User,
                previous.Value.System,
                previous.Value.Idle,
                previous.Value.Nice,
                current.User,
                current.System,
                current.Idle,
                current.Nice);
        }
    }

    [DllImport(
        "libSystem.B.dylib",
        EntryPoint = "sysctlbyname",
        SetLastError = true,
        CharSet = CharSet.Ansi,
        BestFitMapping = false,
        ThrowOnUnmappableChar = true)]
    private static extern int SysctlByName(
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        out ulong oldValue,
        ref nuint oldValueLength,
        IntPtr newValue,
        nuint newValueLength);

    [DllImport("libSystem.B.dylib", EntryPoint = "mach_host_self")]
    private static extern uint MachHostSelf();

    [DllImport("libSystem.B.dylib", EntryPoint = "host_page_size")]
    private static extern int HostPageSize(uint host, out uint pageSize);

    [DllImport("libSystem.B.dylib", EntryPoint = "host_statistics")]
    private static extern int HostStatistics(uint host, int flavor, ref HostCpuLoadStatistics statistics, ref uint count);

    [DllImport("libSystem.B.dylib", EntryPoint = "host_statistics64")]
    private static extern int HostStatistics64(uint host, int flavor, ref VmStatistics64 statistics, ref uint count);

    [StructLayout(LayoutKind.Sequential)]
    private struct VmStatistics64
    {
        public uint FreeCount;
        public uint ActiveCount;
        public uint InactiveCount;
        public uint WireCount;
        public ulong ZeroFillCount;
        public ulong Reactivations;
        public ulong PageIns;
        public ulong PageOuts;
        public ulong Faults;
        public ulong CopyOnWriteFaults;
        public ulong Lookups;
        public ulong Hits;
        public ulong Purges;
        public uint PurgeableCount;
        public uint SpeculativeCount;
        public ulong Decompressions;
        public ulong Compressions;
        public ulong SwapIns;
        public ulong SwapOuts;
        public uint CompressorPageCount;
        public uint ThrottledCount;
        public uint ExternalPageCount;
        public uint InternalPageCount;
        public ulong TotalUncompressedPagesInCompressor;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HostCpuLoadStatistics
    {
        public uint User;
        public uint System;
        public uint Idle;
        public uint Nice;
    }

    private readonly record struct CpuTicks(uint User, uint System, uint Idle, uint Nice);
}
