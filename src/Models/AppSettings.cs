namespace SidebarDiagnostics.App.Models;

public sealed record AppSettings
{
    public static AppSettings Default { get; } = new();

    public int RefreshIntervalMilliseconds { get; init; } = 1000;
    public double CpuAlertThreshold { get; init; } = 85;
    public double MemoryAlertThreshold { get; init; } = 85;
    public double StorageAlertThreshold { get; init; } = 90;
    public double NetworkAlertThreshold { get; init; } = 90;
    public bool AlwaysOnTop { get; init; } = true;
    public bool LaunchAtLogin { get; init; }
    public bool StartMinimized { get; init; }
    public bool ShowMachineName { get; init; } = true;
    public bool ShowClock { get; init; } = true;
    public bool Use24HourClock { get; init; } = true;
    public bool UseFahrenheit { get; init; }
    public int SidebarWidth { get; init; } = 360;
    public double BackgroundOpacity { get; init; } = 1;

    public AppSettings Normalize() => this with
    {
        RefreshIntervalMilliseconds = Math.Clamp(RefreshIntervalMilliseconds, 250, 10000),
        CpuAlertThreshold = Math.Clamp(CpuAlertThreshold, 1, 100),
        MemoryAlertThreshold = Math.Clamp(MemoryAlertThreshold, 1, 100),
        StorageAlertThreshold = Math.Clamp(StorageAlertThreshold, 1, 100),
        NetworkAlertThreshold = Math.Clamp(NetworkAlertThreshold, 1, 100),
        SidebarWidth = Math.Clamp(SidebarWidth, 320, 640),
        BackgroundOpacity = Math.Clamp(BackgroundOpacity, 0.35, 1)
    };
}
