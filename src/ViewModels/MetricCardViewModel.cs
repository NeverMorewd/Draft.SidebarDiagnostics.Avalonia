using CommunityToolkit.Mvvm.ComponentModel;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class MetricCardViewModel(
    string title,
    string value,
    string detail,
    string accentColor,
    double alertThreshold) : ObservableObject
{
    public string Title { get; } = title;
    public string AccentColor { get; } = accentColor;
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
    public string EffectiveAccentColor => IsWarning ? "#FB7185" : AccentColor;

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
