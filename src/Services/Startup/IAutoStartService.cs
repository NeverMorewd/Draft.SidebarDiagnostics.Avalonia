namespace SidebarDiagnostics.App.Services.Startup;

public interface IAutoStartService
{
    ValueTask SetEnabledAsync(bool enabled, CancellationToken cancellationToken);
}
