using System.Security;

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
        var escapedPath = SecurityElement.Escape(executablePath);
        var content = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
              <dict>
                <key>Label</key>
                <string>net.avaloniaui.sidebardiagnostics</string>
                <key>ProgramArguments</key>
                <array><string>{escapedPath}</string></array>
                <key>RunAtLoad</key>
                <true/>
              </dict>
            </plist>
            """;
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }
}
