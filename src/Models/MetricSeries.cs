namespace SidebarDiagnostics.App.Models;

public sealed class MetricSeries(string id)
{
    private readonly List<MetricSample> _samples = [];
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(15);

    public string Id { get; } = id;
    public string Title { get; private set; } = string.Empty;
    public string Subtitle { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string AccentColor { get; private set; } = "#7DD3FC";
    public double CurrentValue { get; private set; }

    public event EventHandler? Changed;

    public void Update(
        string title,
        string subtitle,
        string unit,
        string accentColor,
        double value,
        DateTimeOffset timestamp)
    {
        if (!double.IsFinite(value))
        {
            return;
        }

        Title = title;
        Subtitle = subtitle;
        Unit = unit;
        AccentColor = accentColor;
        CurrentValue = value;
        _samples.Add(new(timestamp, value));
        var cutoff = timestamp - Retention;
        _samples.RemoveAll(sample => sample.Timestamp < cutoff);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public IReadOnlyList<MetricSample> GetSamples(TimeSpan duration, DateTimeOffset now)
    {
        var cutoff = now - duration;
        return _samples.Where(sample => sample.Timestamp >= cutoff).ToArray();
    }
}
