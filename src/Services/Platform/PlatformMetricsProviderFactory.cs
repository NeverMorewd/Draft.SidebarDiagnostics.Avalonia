namespace SidebarDiagnostics.App.Services.Platform;

public static class PlatformMetricsProviderFactory
{
    public static IPlatformMetricsProvider Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsMetricsProvider();
        if (OperatingSystem.IsLinux()) return new LinuxMetricsProvider();
        if (OperatingSystem.IsMacOS()) return new MacOsMetricsProvider();

        throw new PlatformNotSupportedException("Sidebar Diagnostics supports Windows, macOS, and Linux.");
    }
}
