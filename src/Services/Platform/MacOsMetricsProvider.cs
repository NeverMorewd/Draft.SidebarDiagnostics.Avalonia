using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SidebarDiagnostics.App.Services.Platform;

public sealed class MacOsMetricsProvider : IPlatformMetricsProvider
{
    private const int HostVmInfo64 = 4;

    public ValueTask<PlatformMetrics> SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var loadAverages = new double[3];
        if (GetLoadAverage(loadAverages, loadAverages.Length) < 1)
        {
            throw new InvalidOperationException("macOS did not return a system load average.");
        }

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

        var usedPages = statistics.ActiveCount + statistics.InactiveCount + statistics.WireCount + statistics.CompressorPageCount;
        var usedMemory = checked((long)(usedPages * pageSize));
        var cpuUsage = loadAverages[0] * 100d / Environment.ProcessorCount;

        return ValueTask.FromResult(new PlatformMetrics(
            Math.Clamp(cpuUsage, 0, 100),
            usedMemory,
            checked((long)totalMemory)));
    }

    [DllImport("libSystem.B.dylib", EntryPoint = "getloadavg")]
    private static extern int GetLoadAverage([Out] double[] loadAverage, int count);

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
    private static extern int HostPageSize(uint host, out ulong pageSize);

    [DllImport("libSystem.B.dylib", EntryPoint = "host_statistics64")]
    private static extern int HostStatistics64(uint host, int flavor, ref VmStatistics64 statistics, ref uint count);

    [StructLayout(LayoutKind.Sequential)]
    private struct VmStatistics64
    {
        public ulong FreeCount;
        public ulong ActiveCount;
        public ulong InactiveCount;
        public ulong WireCount;
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
        public ulong CompressorPageCount;
        public ulong ThrottledCount;
        public ulong ExternalPageCount;
        public ulong InternalPageCount;
        public ulong TotalUncompressedPagesInCompressor;
    }
}
