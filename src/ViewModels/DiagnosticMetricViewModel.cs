using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class DiagnosticMetricViewModel : ViewModelBase
{
    public DiagnosticMetricViewModel(DiagnosticMetric snapshot)
    {
        Id = snapshot.Label;
        Apply(snapshot);
    }

    public string Id { get; }

    [ObservableProperty]
    public partial string Label { get; private set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; private set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGraph))]
    public partial string? SeriesId { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGraph))]
    public partial double? NumericValue { get; private set; }

    [ObservableProperty]
    public partial string Unit { get; private set; } = string.Empty;

    public bool CanGraph => !string.IsNullOrWhiteSpace(SeriesId) && NumericValue is not null;

    internal void Apply(DiagnosticMetric snapshot)
    {
        Label = snapshot.Label;
        Value = snapshot.Value;
        SeriesId = snapshot.SeriesId;
        NumericValue = snapshot.NumericValue;
        Unit = snapshot.Unit;
    }
}
