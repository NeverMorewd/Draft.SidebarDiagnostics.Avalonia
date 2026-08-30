namespace SidebarDiagnostics.App.Services.Startup;

public sealed class MacOsAutoStartService : IAutoStartService
{
    public async ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library",
            "LaunchAgents",
            "net.avaloniaui.sidebardiagnostics.plist");

        if (!enabled)
        {
            File.Delete(filePath);
            return;
        }

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The application executable path is unavailable.");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var content = AutoStartFileContent.CreateMacOsLaunchAgent(executablePath);
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }
}
