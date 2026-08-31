using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class DiagnosticSectionViewModel : ViewModelBase
{
    private readonly Dictionary<string, DiagnosticMetricViewModel> _metricsById =
        new(StringComparer.OrdinalIgnoreCase);

    public DiagnosticSectionViewModel(DiagnosticSection snapshot)
    {
        Id = snapshot.Id;
        Apply(snapshot);
    }

    public string Id { get; }

    [ObservableProperty]
    public partial string Title { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Subtitle { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string AccentResourceKey { get; private set; } = string.Empty;

    public ObservableCollection<DiagnosticMetricViewModel> Metrics { get; } = [];

    internal void Apply(DiagnosticSection snapshot)
    {
        Title = snapshot.Title;
        Subtitle = snapshot.Subtitle;
        AccentResourceKey = snapshot.AccentResourceKey;
        ReconcileMetrics(snapshot.Metrics);
    }

    internal void RefreshThemeResource() => OnPropertyChanged(nameof(AccentResourceKey));

    private void ReconcileMetrics(IReadOnlyList<DiagnosticMetric> snapshots)
    {
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < snapshots.Count; index++)
        {
            var snapshot = snapshots[index];
            var id = snapshot.Label;
            if (!activeIds.Add(id))
            {
                continue;
            }

            if (!_metricsById.TryGetValue(id, out var metric))
            {
                metric = new DiagnosticMetricViewModel(snapshot);
                _metricsById.Add(id, metric);
                Metrics.Insert(Math.Min(index, Metrics.Count), metric);
                continue;
            }

            metric.Apply(snapshot);
            var currentIndex = Metrics.IndexOf(metric);
            if (currentIndex >= 0 && currentIndex != index)
            {
                Metrics.Move(currentIndex, index);
            }
        }

        for (var index = Metrics.Count - 1; index >= 0; index--)
        {
            var metric = Metrics[index];
            if (activeIds.Contains(metric.Id))
            {
                continue;
            }

            Metrics.RemoveAt(index);
            _metricsById.Remove(metric.Id);
        }
    }
}
