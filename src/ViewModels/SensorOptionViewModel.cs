using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed partial class SensorOptionViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string? CustomName { get; set; }

    [ObservableProperty]
    public partial bool IsVisible { get; set; }

    [ObservableProperty]
    public partial bool IsPinned { get; set; }

    [ObservableProperty]
    public partial bool IsMatch { get; set; } = true;

    public string SensorId { get; }
    public string Device { get; }
    public string Sensor { get; }
    public string Type { get; }
    public string Availability { get; }

    public event Action<SensorOptionViewModel, int>? MoveRequested;

    public SensorOptionViewModel(SensorCatalogEntry entry)
    {
        SensorId = entry.SensorId;
        Device = entry.Device;
        Sensor = entry.Sensor;
        Type = entry.Type.ToString();
        Availability = entry.IsAvailable ? "Available" : "Unavailable";
        CustomName = entry.CustomName;
        IsVisible = entry.IsVisible;
        IsPinned = entry.IsPinned;
    }

    public void UpdateSearch(string searchText)
    {
        IsMatch = string.IsNullOrWhiteSpace(searchText)
            || Device.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Sensor.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || Type.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || (CustomName?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public SensorPreference ToPreference(int sortOrder) => new()
    {
        SensorId = SensorId,
        CustomName = CustomName,
        IsVisible = IsVisible,
        IsPinned = IsPinned,
        SortOrder = sortOrder
    };

    [RelayCommand]
    private void MoveUp() => MoveRequested?.Invoke(this, -1);

    [RelayCommand]
    private void MoveDown() => MoveRequested?.Invoke(this, 1);
}
