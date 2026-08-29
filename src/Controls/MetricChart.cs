using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Controls;

public sealed class MetricChart : Control
{
    public static readonly StyledProperty<MetricSeries?> SeriesProperty =
        AvaloniaProperty.Register<MetricChart, MetricSeries?>(nameof(Series));
    public static readonly StyledProperty<TimeSpan> DurationProperty =
        AvaloniaProperty.Register<MetricChart, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(30));

    public MetricSeries? Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public TimeSpan Duration
    {
        get => GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == SeriesProperty)
        {
            if (change.OldValue is MetricSeries oldSeries) oldSeries.Changed -= OnSeriesChanged;
            if (change.NewValue is MetricSeries newSeries) newSeries.Changed += OnSeriesChanged;
        }

        if (change.Property == SeriesProperty || change.Property == DurationProperty) InvalidateVisual();
        base.OnPropertyChanged(change);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.Parse("#0B111C")), null, bounds, 12);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#223047")), 1);
        for (var index = 1; index < 4; index++)
        {
            var y = bounds.Height * index / 4;
            context.DrawLine(gridPen, new Point(0, y), new Point(bounds.Width, y));
        }

        var samples = Series?.GetSamples(Duration, DateTimeOffset.Now);
        if (samples is null || samples.Count < 2 || bounds.Width <= 0 || bounds.Height <= 0) return;
        var minimum = samples.Min(sample => sample.Value);
        var maximum = samples.Max(sample => sample.Value);
        var range = maximum - minimum;
        if (range < 0.001)
        {
            var padding = Math.Max(Math.Abs(maximum) * 0.1, 1);
            minimum -= padding;
            maximum += padding;
            range = maximum - minimum;
        }
        else
        {
            var padding = range * 0.12;
            minimum -= padding;
            maximum += padding;
            range = maximum - minimum;
        }

        var end = DateTimeOffset.Now;
        var start = end - Duration;
        var geometry = new StreamGeometry();
        using (var drawing = geometry.Open())
        {
            for (var index = 0; index < samples.Count; index++)
            {
                var sample = samples[index];
                var x = Math.Clamp((sample.Timestamp - start).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1) * bounds.Width;
                var y = bounds.Height - ((sample.Value - minimum) / range * bounds.Height);
                if (index == 0) drawing.BeginFigure(new Point(x, y), false);
                else drawing.LineTo(new Point(x, y));
            }
        }

        var color = Color.TryParse(Series!.AccentColor, out var parsed) ? parsed : Colors.DeepSkyBlue;
        context.DrawGeometry(null, new Pen(new SolidColorBrush(color), 2), geometry);
    }

    private void OnSeriesChanged(object? sender, EventArgs e) => InvalidateVisual();
}
