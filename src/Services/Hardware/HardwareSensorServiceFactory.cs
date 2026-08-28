namespace SidebarDiagnostics.App.Services.Hardware;

public static class HardwareSensorServiceFactory
{
    public static IHardwareSensorService Create()
    {
        if (OperatingSystem.IsWindows()) return new WindowsHardwareSensorService();
        if (OperatingSystem.IsLinux()) return new LinuxHardwareSensorService();
        if (OperatingSystem.IsMacOS()) return new MacOsHardwareSensorService();
        return new UnsupportedHardwareSensorService("Hardware temperatures are not available through public macOS APIs.");
    }
}
