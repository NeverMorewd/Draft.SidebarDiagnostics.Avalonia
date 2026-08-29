using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Views;

public partial class MetricChartWindow : Window
{
    private static readonly (string Label, TimeSpan Duration)[] Durations =
    [
        ("15 seconds", TimeSpan.FromSeconds(15)),
        ("30 seconds", TimeSpan.FromSeconds(30)),
        ("1 minute", TimeSpan.FromMinutes(1)),
        ("5 minutes", TimeSpan.FromMinutes(5))
    ];
    private MetricSeries? _series;

    public MetricChartWindow()
    {
        InitializeComponent();
        DragRegion.AddHandler(InputElement.PointerPressedEvent, BeginWindowDrag, RoutingStrategies.Tunnel);
    }

    public MetricChartWindow(MetricSeries series)
        : this()
    {
        _series = series;
        Title = $"{series.Title} · {series.Subtitle}";
        TitleText.Text = series.Title;
        SubtitleText.Text = series.Subtitle;
        Chart.Series = series;
        DurationSelector.ItemsSource = Durations.Select(item => item.Label).ToArray();
        DurationSelector.SelectedIndex = 1;
        series.Changed += OnSeriesChanged;
        Closed += OnClosed;
        UpdateValue();
    }

    private void OnSeriesChanged(object? sender, EventArgs e) => UpdateValue();
    private void UpdateValue()
    {
        if (_series is not null) CurrentValueText.Text = $"{_series.CurrentValue:F1} {_series.Unit}".TrimEnd();
    }

    private void ChangeDuration(object? sender, SelectionChangedEventArgs e)
    {
        if (DurationSelector.SelectedIndex >= 0) Chart.Duration = Durations[DurationSelector.SelectedIndex].Duration;
    }

    private void TogglePin(object? sender, RoutedEventArgs e)
    {
        Topmost = !Topmost;
        if (Topmost) PinButton.Classes.Add("pinned");
        else PinButton.Classes.Remove("pinned");
    }

    private void BeginWindowDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void CloseWindow(object? sender, RoutedEventArgs e) => Close();

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_series is not null) _series.Changed -= OnSeriesChanged;
        Closed -= OnClosed;
    }
}
