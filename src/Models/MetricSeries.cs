namespace SidebarDiagnostics.App.Models;

public sealed class MetricSeries(string id)
{
    private List<MetricSample> _samples = [];
    private static readonly TimeSpan Retention = TimeSpan.FromMinutes(5);
    private DateTimeOffset? _lastTimestamp;

    public string Id { get; } = id;
    public string Title { get; private set; } = string.Empty;
    public string Subtitle { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string AccentResourceKey { get; private set; } = Styling.ThemeResourceKeys.CpuAccent;
    public double CurrentValue { get; private set; }
    public bool IsRecording { get; private set; }

    public event EventHandler? Changed;

    public void Update(
        string title,
        string subtitle,
        string unit,
        string accentResourceKey,
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
        AccentResourceKey = accentResourceKey;
        CurrentValue = value;
        _lastTimestamp = timestamp;
        if (IsRecording)
        {
            _samples.Add(new(timestamp, value));
            var cutoff = timestamp - Retention;
            _samples.RemoveAll(sample => sample.Timestamp < cutoff);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void StartRecording()
    {
        if (IsRecording)
        {
            return;
        }

        IsRecording = true;
        _samples = [];
        if (_lastTimestamp is { } timestamp)
        {
            _samples.Add(new(timestamp, CurrentValue));
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void StopRecording()
    {
        if (!IsRecording)
        {
            return;
        }

        IsRecording = false;
        _samples = [];
    }

    public IReadOnlyList<MetricSample> GetSamples(TimeSpan duration, DateTimeOffset now)
    {
        var cutoff = now - duration;
        return _samples.Where(sample => sample.Timestamp >= cutoff).ToArray();
    }
}
