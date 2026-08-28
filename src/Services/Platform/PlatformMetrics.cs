namespace SidebarDiagnostics.App.Services.Platform;

public readonly record struct PlatformMetrics(
    double CpuUsagePercent,
    long MemoryUsedBytes,
    long MemoryTotalBytes);
