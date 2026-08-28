using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.ExternalMetrics;

public interface IExternalMetricService : IDisposable
{
    ValueTask<IReadOnlyList<ExternalMetricSnapshot>> ReadAsync(
        IReadOnlyList<ExternalMetricDefinition> definitions,
        CancellationToken cancellationToken);

    ValueTask<ExternalMetricSnapshot> PreviewAsync(
        ExternalMetricDefinition definition,
        CancellationToken cancellationToken);
}
