using System.Net.NetworkInformation;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Platform;

namespace SidebarDiagnostics.App.Services;

public sealed class SystemMetricsService : ISystemMetricsService
{
    private readonly IPlatformMetricsProvider _platformMetricsProvider;
    private readonly object _syncRoot = new();
    private DateTimeOffset _lastSampledAt = DateTimeOffset.UtcNow;
    private long _lastReceivedBytes;
    private long _lastSentBytes;

    public SystemMetricsService()
        : this(PlatformMetricsProviderFactory.Create())
    {
    }

    public SystemMetricsService(IPlatformMetricsProvider platformMetricsProvider)
    {
        _platformMetricsProvider = platformMetricsProvider;
        (_lastReceivedBytes, _lastSentBytes) = ReadNetworkTotals();
    }

    public async ValueTask<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var platformMetrics = await _platformMetricsProvider.SampleAsync(cancellationToken);

        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var elapsed = Math.Max((now - _lastSampledAt).TotalSeconds, 0.001);
            var memoryUsage = platformMetrics.MemoryTotalBytes > 0
                ? platformMetrics.MemoryUsedBytes * 100d / platformMetrics.MemoryTotalBytes
                : 0;
            var storageUsage = ReadStorageUsage();

            var (receivedBytes, sentBytes) = ReadNetworkTotals();
            var downloadRate = Math.Max(0, receivedBytes - _lastReceivedBytes) / elapsed;
            var uploadRate = Math.Max(0, sentBytes - _lastSentBytes) / elapsed;
            var networkActivity = Math.Min(100, (downloadRate + uploadRate) / 1_250_000d * 100);

            _lastSampledAt = now;
            _lastReceivedBytes = receivedBytes;
            _lastSentBytes = sentBytes;

            return new SystemMetricsSnapshot(
                now,
                SystemMetricsSnapshot.Empty.Platform,
                platformMetrics.CpuUsagePercent,
                platformMetrics.MemoryUsedBytes,
                platformMetrics.MemoryTotalBytes,
                Math.Clamp(memoryUsage, 0, 100),
                storageUsage,
                downloadRate,
                uploadRate,
                networkActivity);
        }
    }

    private static double ReadStorageUsage()
    {
        var root = Path.GetPathRoot(AppContext.BaseDirectory);
        if (string.IsNullOrWhiteSpace(root)) return 0;

        var drive = new DriveInfo(root);
        if (!drive.IsReady || drive.TotalSize <= 0) return 0;
        return (drive.TotalSize - drive.AvailableFreeSpace) * 100d / drive.TotalSize;
    }

    private static (long Received, long Sent) ReadNetworkTotals()
    {
        long received = 0;
        long sent = 0;

        foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (networkInterface.OperationalStatus != OperationalStatus.Up ||
                networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
            {
                continue;
            }

            try
            {
                var statistics = networkInterface.GetIPStatistics();
                received += statistics.BytesReceived;
                sent += statistics.BytesSent;
            }
            catch (Exception exception) when (exception is NetworkInformationException or PlatformNotSupportedException)
            {
            }
        }

        return (received, sent);
    }

    public void Dispose()
    {
    }
}
