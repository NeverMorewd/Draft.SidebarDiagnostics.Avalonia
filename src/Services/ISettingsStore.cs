using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services;

public interface ISettingsStore
{
    ValueTask<AppSettings> LoadAsync(CancellationToken cancellationToken);
    ValueTask SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
