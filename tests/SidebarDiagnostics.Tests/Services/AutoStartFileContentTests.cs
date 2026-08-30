using System.Xml.Linq;
using SidebarDiagnostics.App.Services.Startup;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class AutoStartFileContentTests
{
    [Fact]
    public void LinuxEntryEscapesDesktopExecReservedCharacters()
    {
        var content = AutoStartFileContent.CreateLinuxDesktopEntry("/opt/Side $`%\\\"bar/Sidebar Diagnostics");
        var escapedPath = "/opt/Side "
            + new string('\\', 2) + "$"
            + new string('\\', 2) + "`"
            + "%%"
            + new string('\\', 6) + "\"bar/Sidebar Diagnostics";

        Assert.Contains($"Exec=\"{escapedPath}\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void LinuxEntryRejectsLineBreaks()
    {
        Assert.Throws<ArgumentException>(() => AutoStartFileContent.CreateLinuxDesktopEntry("/opt/app\ninvalid"));
    }

    [Fact]
    public void MacOsLaunchAgentPreservesExecutablePath()
    {
        const string path = "/Applications/Sidebar & Diagnostics <preview>.app/Contents/MacOS/SidebarDiagnostics.App";

        var document = XDocument.Parse(AutoStartFileContent.CreateMacOsLaunchAgent(path));
        var argument = document.Descendants("array").Elements("string").Single().Value;

        Assert.Equal(path, argument);
    }
}
