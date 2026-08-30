using System.Net.NetworkInformation;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;
using SidebarDiagnostics.App.Services.Platform;

namespace SidebarDiagnostics.App.Services;

public sealed class SystemMetricsService : ISystemMetricsService
{
    private readonly IPlatformMetricsProvider _platformMetricsProvider;
    private readonly object _syncRoot = new();
    private readonly NetworkTrafficRateTracker _networkTraffic = new();

    public SystemMetricsService()
        : this(PlatformMetricsProviderFactory.Create())
    {
    }

    public SystemMetricsService(IPlatformMetricsProvider platformMetricsProvider)
    {
        _platformMetricsProvider = platformMetricsProvider;
    }

    public async ValueTask<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var platformMetrics = await _platformMetricsProvider.SampleAsync(cancellationToken);

        lock (_syncRoot)
        {
            var now = DateTimeOffset.UtcNow;
            var memoryUsage = platformMetrics.MemoryTotalBytes > 0
                ? platformMetrics.MemoryUsedBytes * 100d / platformMetrics.MemoryTotalBytes
                : 0;
            var storage = ReadStorage();

            var networkRate = _networkTraffic.Update(ReadPrimaryNetworkTraffic(), now);
            var downloadRate = networkRate.DownloadBytesPerSecond;
            var uploadRate = networkRate.UploadBytesPerSecond;
            var networkActivity = Math.Min(100, (downloadRate + uploadRate) / 1_250_000d * 100);

            return new SystemMetricsSnapshot(
                now,
                SystemMetricsSnapshot.Empty.Platform,
                platformMetrics.CpuUsagePercent,
                platformMetrics.MemoryUsedBytes,
                platformMetrics.MemoryTotalBytes,
                Math.Clamp(memoryUsage, 0, 100),
                storage.UsagePercent,
                storage.UsedBytes,
                storage.TotalBytes,
                downloadRate,
                uploadRate,
                networkActivity);
        }
    }

    private static (double UsagePercent, long UsedBytes, long TotalBytes) ReadStorage()
    {
        var root = Path.GetPathRoot(AppContext.BaseDirectory);
        if (string.IsNullOrWhiteSpace(root)) return (0, 0, 0);

        var drive = new DriveInfo(root);
        if (!drive.IsReady || drive.TotalSize <= 0) return (0, 0, 0);
        var used = drive.TotalSize - drive.AvailableFreeSpace;
        return (used * 100d / drive.TotalSize, used, drive.TotalSize);
    }

    private static NetworkTrafficSample? ReadPrimaryNetworkTraffic()
    {
        var networks = DiagnosticDeviceSelection.SelectPrimaryNetworks(NetworkInterface.GetAllNetworkInterfaces());
        if (networks.Count == 0)
        {
            return null;
        }

        var networkInterface = networks[0];

        try
        {
            var statistics = networkInterface.GetIPStatistics();
            return new NetworkTrafficSample(networkInterface.Id, statistics.BytesReceived, statistics.BytesSent);
        }
        catch (Exception exception) when (exception is NetworkInformationException or PlatformNotSupportedException)
        {
            return null;
        }
    }

    public void Dispose()
    {
    }
}
