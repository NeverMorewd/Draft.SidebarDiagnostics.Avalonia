using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class SensorCatalogTests
{
    [Fact]
    public void SelectVisibleCoalescesDuplicateProviderIds()
    {
        var first = Reading("duplicate", "Core 1 Load") with { Value = 10 };
        var second = Reading("duplicate", "Core 1 Load") with { Value = 20 };

        var selected = SensorCatalog.SelectVisible([first, second], []);

        Assert.Single(selected);
        Assert.Equal(10, selected[0].Value);
    }
    [Fact]
    public void BuildPreservesUnavailablePreferences()
    {
        SensorPreference[] preferences =
        [
            new()
            {
                SensorId = "missing",
                CustomName = "Coolant",
                IsPinned = true,
                SortOrder = 0
            }
        ];

        var entry = Assert.Single(SensorCatalog.Build([], preferences));

        Assert.False(entry.IsAvailable);
        Assert.Equal("Coolant", entry.DisplayName);
        Assert.True(entry.IsPinned);
    }

    [Fact]
    public void SelectVisibleAppliesVisibilityPinningAndOrder()
    {
        HardwareSensorReading[] readings =
        [
            Reading("cpu", "CPU"),
            Reading("fan", "Fan"),
            Reading("water", "Water")
        ];
        SensorPreference[] preferences =
        [
            Preference("cpu", true, false, 0),
            Preference("fan", false, false, 1),
            Preference("water", true, true, 2)
        ];

        var selected = SensorCatalog.SelectVisible(readings, preferences);

        Assert.Collection(
            selected,
            reading => Assert.Equal("water", reading.Id),
            reading => Assert.Equal("cpu", reading.Id));
    }

    [Fact]
    public void BuildUsesStableIdentityWhenNamesChange()
    {
        var preference = Preference("device:sensor", true, false, 0) with { CustomName = "Package" };
        var reading = Reading("device:sensor", "Renamed upstream sensor");

        var entry = Assert.Single(SensorCatalog.Build([reading], [preference]));

        Assert.True(entry.IsAvailable);
        Assert.Equal("Package", entry.DisplayName);
    }

    [Fact]
    public void DefaultSelectionIsDeterministicAndBounded()
    {
        HardwareSensorReading[] readings =
        [
            Reading("z", "Z"),
            Reading("a", "A"),
            Reading("b", "B")
        ];

        var selected = SensorCatalog.SelectVisible(readings, [], 2);

        Assert.Equal(["a", "b"], selected.Select(reading => reading.Id));
    }

    [Fact]
    public void InitialCatalogShowsAllDiscoveredSensors()
    {
        var readings = Enumerable.Range(0, 15)
            .Select(index => Reading($"sensor-{index:D2}", $"Sensor {index:D2}"))
            .ToArray();

        var catalog = SensorCatalog.Build(readings, []);

        Assert.Equal(15, catalog.Count(entry => entry.IsVisible));
    }

    private static HardwareSensorReading Reading(string id, string name) =>
        new(
            id,
            "device",
            "Device",
            HardwareDeviceType.Unknown,
            HardwareVendor.Unknown,
            name,
            HardwareSensorType.Temperature,
            42,
            "°C");

    private static SensorPreference Preference(string id, bool isVisible, bool isPinned, int order) => new()
    {
        SensorId = id,
        IsVisible = isVisible,
        IsPinned = isPinned,
        SortOrder = order
    };
}
