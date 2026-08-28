using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services;

public interface ISystemMetricsService : IDisposable
{
    ValueTask<SystemMetricsSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
}
