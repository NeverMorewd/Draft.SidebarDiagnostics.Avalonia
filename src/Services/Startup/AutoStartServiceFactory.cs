namespace SidebarDiagnostics.App.Services.Startup;

public static class AutoStartServiceFactory
{
    public static IAutoStartService Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsAutoStartService();
        if (OperatingSystem.IsLinux()) return new LinuxAutoStartService();
        if (OperatingSystem.IsMacOS()) return new MacOsAutoStartService();
        throw new PlatformNotSupportedException();
    }
}
