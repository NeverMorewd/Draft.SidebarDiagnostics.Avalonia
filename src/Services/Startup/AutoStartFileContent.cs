using System.Security;
using System.Text;

namespace SidebarDiagnostics.App.Services.Startup;

internal static class AutoStartFileContent
{
    public static string CreateLinuxDesktopEntry(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        if (executablePath.IndexOfAny(['\r', '\n']) >= 0)
        {
            throw new ArgumentException("The executable path cannot contain line breaks.", nameof(executablePath));
        }

        return $"""
            [Desktop Entry]
            Type=Application
            Name=Sidebar Diagnostics
            Exec="{EscapeDesktopExecArgument(executablePath)}"
            Terminal=false
            X-GNOME-Autostart-enabled=true
            """;
    }

    public static string CreateMacOsLaunchAgent(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        var escapedPath = SecurityElement.Escape(executablePath)
            ?? throw new ArgumentException("The executable path is invalid.", nameof(executablePath));
        return $"""
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
    }

    private static string EscapeDesktopExecArgument(string value)
    {
        var result = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            switch (character)
            {
                case '\\':
                    result.Append('\\', 4);
                    break;
                case '"':
                case '`':
                case '$':
                    result.Append('\\', 2).Append(character);
                    break;
                case '%':
                    result.Append("%%");
                    break;
                default:
                    result.Append(character);
                    break;
            }
        }

        return result.ToString();
    }
}
