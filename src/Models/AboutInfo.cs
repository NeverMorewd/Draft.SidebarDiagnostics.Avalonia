using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;

namespace SidebarDiagnostics.App.Models;

public sealed record AboutInfo(
    string ApplicationVersion,
    string AvaloniaVersion,
    string DotNetVersion,
    string Author,
    string License,
    string RepositoryUrl,
    string IssuesUrl)
{
    public const string Repository = "https://github.com/NeverMorewd/SidebarDiagnostics.Avalonia";
    public const string Issues = $"{Repository}/issues";

    public static AboutInfo Create() => new(
        GetVersion(typeof(SidebarDiagnostics.App.App).Assembly),
        GetVersion(typeof(Application).Assembly),
        RuntimeInformation.FrameworkDescription,
        "NeverMorewd; original project by ArcadeRenegade",
        "GPL-3.0-only",
        Repository,
        Issues);

    private static string GetVersion(Assembly assembly)
    {
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var version = informational ?? assembly.GetName().Version?.ToString() ?? "Unknown";
        var metadataIndex = version.IndexOf('+', StringComparison.Ordinal);
        return metadataIndex >= 0 ? version[..metadataIndex] : version;
    }
}
