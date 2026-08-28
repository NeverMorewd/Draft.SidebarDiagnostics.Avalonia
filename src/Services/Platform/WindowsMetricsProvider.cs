using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SidebarDiagnostics.App.Services.Platform;

public sealed class WindowsMetricsProvider : IPlatformMetricsProvider
{
    private ulong _previousIdle;
    private ulong _previousTotal;
    private bool _hasPreviousSample;

    public ValueTask<PlatformMetrics> SampleAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!GetSystemTimes(out var idleTime, out var kernelTime, out var userTime))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var memoryStatus = new MemoryStatusEx();
        if (!GlobalMemoryStatusEx(ref memoryStatus))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var idle = idleTime.ToUInt64();
        var total = kernelTime.ToUInt64() + userTime.ToUInt64();
        var cpuUsage = 0d;

        if (_hasPreviousSample)
        {
            var totalDelta = total - _previousTotal;
            var idleDelta = idle - _previousIdle;
            if (totalDelta > 0)
            {
                cpuUsage = (totalDelta - idleDelta) * 100d / totalDelta;
            }
        }

        _previousIdle = idle;
        _previousTotal = total;
        _hasPreviousSample = true;

        var usedMemory = checked((long)(memoryStatus.TotalPhysical - memoryStatus.AvailablePhysical));
        var totalMemory = checked((long)memoryStatus.TotalPhysical);
        return ValueTask.FromResult(new PlatformMetrics(Math.Clamp(cpuUsage, 0, 100), usedMemory, totalMemory));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetSystemTimes(out FileTime idleTime, out FileTime kernelTime, out FileTime userTime);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct FileTime
    {
        private readonly uint _low;
        private readonly uint _high;

        public ulong ToUInt64() => ((ulong)_high << 32) | _low;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;

        public MemoryStatusEx()
        {
        }
    }
}
