namespace SidebarDiagnostics.App.Services.Platform;

public interface IPlatformMetricsProvider
{
    ValueTask<PlatformMetrics> SampleAsync(CancellationToken cancellationToken);
}
