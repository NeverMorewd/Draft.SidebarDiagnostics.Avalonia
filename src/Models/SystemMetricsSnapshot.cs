namespace SidebarDiagnostics.App.Models;

public sealed record SystemMetricsSnapshot(
    DateTimeOffset Timestamp,
    string Platform,
    double CpuUsagePercent,
    long MemoryUsedBytes,
    double MemoryUsagePercent,
    double StorageUsagePercent,
    double DownloadBytesPerSecond,
    double UploadBytesPerSecond,
    double NetworkActivityPercent)
{
    public static SystemMetricsSnapshot Empty { get; } = new(
        DateTimeOffset.UtcNow,
        GetPlatformName(),
        0,
        0,
        0,
        0,
        0,
        0,
        0);

    private static string GetPlatformName()
    {
        if (OperatingSystem.IsWindows()) return "Windows";
        if (OperatingSystem.IsMacOS()) return "macOS";
        if (OperatingSystem.IsLinux()) return "Linux";
        return "Unknown platform";
    }
}
