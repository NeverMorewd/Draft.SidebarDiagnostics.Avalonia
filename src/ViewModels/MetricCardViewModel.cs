using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Styling;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class MetricCardViewModel(
    string title,
    string value,
    string detail,
    string accentResourceKey,
    double alertThreshold) : ObservableObject
{
    [ObservableProperty]
    public partial string Title { get; set; } = title;
    public string AccentResourceKey { get; } = accentResourceKey;
    public MetricHistory History { get; } = new(60);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWarning))]
    [NotifyPropertyChangedFor(nameof(EffectiveAccentColor))]
    public partial double AlertThreshold { get; set; } = alertThreshold;

    [ObservableProperty]
    public partial string Value { get; set; } = value;

    [ObservableProperty]
    public partial string Detail { get; set; } = detail;

    [ObservableProperty]
    public partial double Progress { get; set; }

    public bool IsWarning => Progress >= AlertThreshold;
    public string EffectiveAccentColor => IsWarning ? ThemeResourceKeys.WarningAccent : AccentResourceKey;

    public void Update(string value, string detail, double progress)
    {
        Value = value;
        Detail = detail;
        Progress = Math.Clamp(progress, 0, 100);
        History.Add(Progress);
        OnPropertyChanged(nameof(IsWarning));
        OnPropertyChanged(nameof(EffectiveAccentColor));
    }
}
