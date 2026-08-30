namespace SidebarDiagnostics.App.Services.Startup;

public sealed class LinuxAutoStartService : IAutoStartService
{
    public async ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
        {
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        var filePath = Path.Combine(configHome, "autostart", "sidebar-diagnostics.desktop");
        if (!enabled)
        {
            File.Delete(filePath);
            return;
        }

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The application executable path is unavailable.");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var content = AutoStartFileContent.CreateLinuxDesktopEntry(executablePath);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }
}
