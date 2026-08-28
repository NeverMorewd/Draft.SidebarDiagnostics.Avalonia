using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services;
using Xunit;

namespace SidebarDiagnostics.Tests.Models;

public sealed class AppSettingsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"SidebarDiagnostics.Tests.{Guid.NewGuid():N}");

    [Fact]
    public void NormalizeClampsEveryNumericSetting()
    {
        var settings = new AppSettings
        {
            RefreshIntervalMilliseconds = 1,
            CpuAlertThreshold = -1,
            MemoryAlertThreshold = 101,
            StorageAlertThreshold = 0,
            NetworkAlertThreshold = 500,
            GpuAlertThreshold = 500
        };

        var normalized = settings.Normalize();

        Assert.Equal(250, normalized.RefreshIntervalMilliseconds);
        Assert.Equal(1, normalized.CpuAlertThreshold);
        Assert.Equal(100, normalized.MemoryAlertThreshold);
        Assert.Equal(1, normalized.StorageAlertThreshold);
        Assert.Equal(100, normalized.NetworkAlertThreshold);
        Assert.Equal(100, normalized.GpuAlertThreshold);
    }

    [Fact]
    public async Task StoreRoundTripsNormalizedSettings()
    {
        var path = Path.Combine(_directory, "settings.json");
        var store = new JsonSettingsStore(path);
        var expected = new AppSettings
        {
            RefreshIntervalMilliseconds = 750,
            CpuAlertThreshold = 80,
            MemoryAlertThreshold = 81,
            StorageAlertThreshold = 82,
            NetworkAlertThreshold = 83,
            AlwaysOnTop = false,
            LaunchAtLogin = true,
            StartMinimized = true
        };

        await store.SaveAsync(expected, TestContext.Current.CancellationToken);
        var actual = await store.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equivalent(expected, actual);
    }

    [Fact]
    public void NormalizeDeduplicatesAndReordersSensorPreferences()
    {
        var settings = new AppSettings
        {
            SensorPreferences =
            [
                new SensorPreference { SensorId = " sensor-a ", CustomName = " Package ", SortOrder = 10 },
                new SensorPreference { SensorId = "sensor-b", SortOrder = 5 },
                new SensorPreference { SensorId = "sensor-a", CustomName = "Final", SortOrder = 20 },
                new SensorPreference { SensorId = " ", SortOrder = 0 }
            ]
        };

        var preferences = settings.Normalize().SensorPreferences;

        Assert.Collection(
            preferences,
            preference =>
            {
                Assert.Equal("sensor-b", preference.SensorId);
                Assert.Equal(0, preference.SortOrder);
            },
            preference =>
            {
                Assert.Equal("sensor-a", preference.SensorId);
                Assert.Equal("Final", preference.CustomName);
                Assert.Equal(1, preference.SortOrder);
            });
    }

    [Fact]
    public async Task StoreFallsBackToDefaultsForInvalidJson()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        await File.WriteAllTextAsync(path, "not-json", TestContext.Current.CancellationToken);

        var actual = await new JsonSettingsStore(path).LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(AppSettings.Default, actual);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
