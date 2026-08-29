using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Controls;

public sealed class MetricChart : Control
{
    private const double Left = 72, Top = 18, Right = 18, Bottom = 38;
    private static readonly IBrush LabelBrush = new SolidColorBrush(Color.Parse("#71839B"));
    private static readonly Pen GridPen = new(new SolidColorBrush(Color.Parse("#223047")), 1);
    private static readonly Pen AxisPen = new(new SolidColorBrush(Color.Parse("#354760")), 1);
    private static readonly Typeface LabelTypeface = new("Inter");
    public static readonly StyledProperty<MetricSeries?> SeriesProperty = AvaloniaProperty.Register<MetricChart, MetricSeries?>(nameof(Series));
    public static readonly StyledProperty<TimeSpan> DurationProperty = AvaloniaProperty.Register<MetricChart, TimeSpan>(nameof(Duration), TimeSpan.FromSeconds(30));

    public MetricSeries? Series { get => GetValue(SeriesProperty); set => SetValue(SeriesProperty, value); }
    public TimeSpan Duration { get => GetValue(DurationProperty); set => SetValue(DurationProperty, value); }

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
        var plot = new Rect(Left, Top, Math.Max(0, Bounds.Width - Left - Right), Math.Max(0, Bounds.Height - Top - Bottom));
        if (plot.Width <= 0 || plot.Height <= 0) return;
        var now = DateTimeOffset.Now;
        IReadOnlyList<MetricSample> samples = Series?.GetSamples(Duration, now) ?? [];
        var (minimum, maximum) = CalculateRange(samples);
        DrawAxes(context, plot, minimum, maximum, now);
        DrawSeries(context, plot, samples, minimum, maximum, now);
    }

    private void DrawAxes(DrawingContext context, Rect plot, double minimum, double maximum, DateTimeOffset end)
    {
        for (var index = 0; index <= 4; index++)
        {
            var fraction = index / 4d;
            var y = plot.Bottom - plot.Height * fraction;
            context.DrawLine(index == 0 ? AxisPen : GridPen, new(plot.Left, y), new(plot.Right, y));
            var label = CreateLabel(FormatValue(minimum + (maximum - minimum) * fraction, Series?.Unit));
            context.DrawText(label, new(plot.Left - label.Width - 10, y - label.Height / 2));
        }
        for (var index = 0; index <= 3; index++)
        {
            var fraction = index / 3d;
            var x = plot.Left + plot.Width * fraction;
            context.DrawLine(index == 0 ? AxisPen : GridPen, new(x, plot.Top), new(x, plot.Bottom));
            var timestamp = end - Duration + TimeSpan.FromTicks((long)(Duration.Ticks * fraction));
            var label = CreateLabel(timestamp.ToString(Duration <= TimeSpan.FromMinutes(1) ? "HH:mm:ss" : "HH:mm", CultureInfo.CurrentCulture));
            var labelX = Math.Clamp(x - label.Width / 2, plot.Left, plot.Right - label.Width);
            context.DrawText(label, new(labelX, plot.Bottom + 10));
        }
    }

    private void DrawSeries(DrawingContext context, Rect plot, IReadOnlyList<MetricSample> samples, double minimum, double maximum, DateTimeOffset end)
    {
        if (samples.Count < 2) return;
        var start = end - Duration;
        var range = maximum - minimum;
        var geometry = new StreamGeometry();
        using (var drawing = geometry.Open())
        {
            for (var index = 0; index < samples.Count; index++)
            {
                var sample = samples[index];
                var x = plot.Left + Math.Clamp((sample.Timestamp - start).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1) * plot.Width;
                var y = plot.Bottom - (sample.Value - minimum) / range * plot.Height;
                if (index == 0) drawing.BeginFigure(new(x, y), false); else drawing.LineTo(new(x, y));
            }
        }
        var color = Color.TryParse(Series!.AccentColor, out var parsed) ? parsed : Colors.DeepSkyBlue;
        var brush = new SolidColorBrush(color);
        context.DrawGeometry(null, new Pen(brush, 2), geometry);
        var latest = samples[^1];
        var latestX = plot.Left + Math.Clamp((latest.Timestamp - start).TotalMilliseconds / Duration.TotalMilliseconds, 0, 1) * plot.Width;
        var latestY = plot.Bottom - (latest.Value - minimum) / range * plot.Height;
        context.DrawEllipse(brush, new Pen(Brushes.White, 1.5), new(latestX, latestY), 3.5, 3.5);
    }

    internal static (double Minimum, double Maximum) CalculateRange(IReadOnlyList<MetricSample> samples)
    {
        if (samples.Count == 0) return (0, 1);
        var minimum = samples.Min(sample => sample.Value);
        var maximum = samples.Max(sample => sample.Value);
        if (minimum >= 0) minimum = 0;
        var span = maximum - minimum;
        if (span < 0.001) span = Math.Max(Math.Abs(maximum), 1);
        var step = NiceStep(span / 4);
        var axisMinimum = minimum >= 0 ? 0 : Math.Floor(minimum / step) * step;
        step = NiceStep((maximum - axisMinimum <= 0 ? span : maximum - axisMinimum) / 4);
        var axisMaximum = axisMinimum + step * 4;
        return (axisMinimum, axisMaximum);
    }

    private static FormattedText CreateLabel(string text) => new(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, LabelTypeface, 10, LabelBrush);
    private static string FormatValue(double value, string? unit)
    {
        var number = Math.Abs(value) switch
        {
            >= 1000 => value.ToString("N0", CultureInfo.CurrentCulture),
            >= 100 => value.ToString("F0", CultureInfo.CurrentCulture),
            >= 10 => value.ToString("F1", CultureInfo.CurrentCulture),
            _ => value.ToString("F2", CultureInfo.CurrentCulture)
        };
        return string.IsNullOrWhiteSpace(unit) ? number : $"{number} {unit}";
    }

    private static double NiceStep(double value)
    {
        var exponent = Math.Floor(Math.Log10(Math.Max(value, double.Epsilon)));
        var fraction = value / Math.Pow(10, exponent);
        var niceFraction = fraction <= 1 ? 1 : fraction <= 2 ? 2 : fraction <= 2.5 ? 2.5 : fraction <= 5 ? 5 : 10;
        return niceFraction * Math.Pow(10, exponent);
    }
    private void OnSeriesChanged(object? sender, EventArgs e) => InvalidateVisual();
}
