using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Controls;

public sealed class Sparkline : Control
{
    public static readonly StyledProperty<MetricHistory?> ValuesProperty =
        AvaloniaProperty.Register<Sparkline, MetricHistory?>(nameof(Values));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Sparkline, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Sparkline, double>(nameof(StrokeThickness), 1.5);

    static Sparkline()
    {
        AffectsRender<Sparkline>(ValuesProperty, StrokeProperty, StrokeThicknessProperty);
    }

    public MetricHistory? Values
    {
        get => GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ValuesProperty)
        {
            if (change.OldValue is MetricHistory oldHistory)
            {
                oldHistory.Changed -= OnHistoryChanged;
            }

            if (change.NewValue is MetricHistory newHistory)
            {
                newHistory.Changed += OnHistoryChanged;
            }
        }

        base.OnPropertyChanged(change);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var values = Values?.Values;
        if (values is null || values.Count < 2 || Bounds.Width <= 0 || Bounds.Height <= 0 || Stroke is null)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var drawing = geometry.Open())
        {
            for (var index = 0; index < values.Count; index++)
            {
                var x = index * Bounds.Width / (values.Count - 1);
                var y = Bounds.Height - Math.Clamp(values[index], 0, 100) * Bounds.Height / 100;
                var point = new Point(x, y);

                if (index == 0)
                {
                    drawing.BeginFigure(point, false);
                }
                else
                {
                    drawing.LineTo(point);
                }
            }
        }

        context.DrawGeometry(null, new Pen(Stroke, StrokeThickness), geometry);
    }

    private void OnHistoryChanged(object? sender, EventArgs e) => InvalidateVisual();
}
