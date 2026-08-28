using System.Collections.ObjectModel;

namespace SidebarDiagnostics.App.Models;

public sealed class MetricHistory
{
    private readonly int _capacity;

    public MetricHistory(int capacity)
    {
        if (capacity < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "History capacity must be at least two.");
        }

        _capacity = capacity;
        Values = new ObservableCollection<double>();
    }

    public ObservableCollection<double> Values { get; }

    public event EventHandler? Changed;

    public void Add(double value)
    {
        var normalized = double.IsFinite(value) ? Math.Clamp(value, 0, 100) : 0;
        Values.Add(normalized);

        while (Values.Count > _capacity)
        {
            Values.RemoveAt(0);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }
}
