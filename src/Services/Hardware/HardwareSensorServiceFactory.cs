namespace SidebarDiagnostics.App.Services.Hardware;

public static class HardwareSensorServiceFactory
{
    public static IHardwareSensorService Create()
    {
        IHardwareSensorService provider = OperatingSystem.IsWindows()
            ? new WindowsHardwareSensorService()
            : OperatingSystem.IsLinux()
                ? new LinuxHardwareSensorService()
                : OperatingSystem.IsMacOS()
                    ? new MacOsHardwareSensorService()
                    : new UnsupportedHardwareSensorService("Hardware sensors are not supported on this operating system.");
        return new ResilientHardwareSensorService(provider);
    }
}
