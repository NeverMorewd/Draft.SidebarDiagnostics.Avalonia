using Microsoft.Win32;
using System.Runtime.Versioning;

namespace SidebarDiagnostics.App.Services.Startup;

[SupportedOSPlatform("windows")]
public sealed class WindowsAutoStartService : IAutoStartService
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "SidebarDiagnostics";

    public ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var key = Registry.CurrentUser.CreateSubKey(RegistryPath, true);

        if (enabled)
        {
            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("The application executable path is unavailable.");
            key.SetValue(ValueName, $"\"{executablePath}\"");
        }
        else
        {
            key.DeleteValue(ValueName, false);
        }

        return ValueTask.CompletedTask;
    }
}
