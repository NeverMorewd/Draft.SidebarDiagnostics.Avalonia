using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class ExternalMetricOptionViewModel : ViewModelBase
{
    private readonly Func<ExternalMetricDefinition, CancellationToken, ValueTask<ExternalMetricSnapshot>> _preview;

    public string Id { get; }
    public IReadOnlyList<ExternalMetricSourceKind> SourceKinds { get; } = Enum.GetValues<ExternalMetricSourceKind>();

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial ExternalMetricSourceKind SourceKind { get; set; }

    [ObservableProperty]
    public partial string Source { get; set; }

    [ObservableProperty]
    public partial string JsonPath { get; set; }

    [ObservableProperty]
    public partial string Unit { get; set; }

    [ObservableProperty]
    public partial double Minimum { get; set; }

    [ObservableProperty]
    public partial double Maximum { get; set; }

    [ObservableProperty]
    public partial int RefreshIntervalSeconds { get; set; }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    [ObservableProperty]
    public partial string PreviewStatus { get; set; } = "Not tested";

    public event Action<ExternalMetricOptionViewModel>? RemoveRequested;

    public ExternalMetricOptionViewModel(
        ExternalMetricDefinition definition,
        Func<ExternalMetricDefinition, CancellationToken, ValueTask<ExternalMetricSnapshot>> preview)
    {
        _preview = preview;
        Id = definition.Id;
        Title = definition.Title;
        SourceKind = definition.SourceKind;
        Source = definition.Source;
        JsonPath = definition.JsonPath;
        Unit = definition.Unit;
        Minimum = definition.Minimum;
        Maximum = definition.Maximum;
        RefreshIntervalSeconds = definition.RefreshIntervalSeconds;
        IsEnabled = definition.IsEnabled;
    }

    public ExternalMetricDefinition ToDefinition() => new()
    {
        Id = Id,
        Title = Title,
        SourceKind = SourceKind,
        Source = Source,
        JsonPath = JsonPath,
        Unit = Unit,
        Minimum = Minimum,
        Maximum = Maximum,
        RefreshIntervalSeconds = RefreshIntervalSeconds,
        IsEnabled = IsEnabled
    };

    [RelayCommand]
    private async Task TestAsync(CancellationToken cancellationToken)
    {
        PreviewStatus = "Testing";
        var snapshot = await _preview(ToDefinition(), cancellationToken);
        PreviewStatus = snapshot.IsSuccess
            ? $"{snapshot.Value:F2}{snapshot.Unit}"
            : snapshot.Status;
    }

    [RelayCommand]
    private void Remove() => RemoveRequested?.Invoke(this);
}
