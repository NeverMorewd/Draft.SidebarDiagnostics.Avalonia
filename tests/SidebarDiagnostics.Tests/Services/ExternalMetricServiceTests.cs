using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.ExternalMetrics;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class ExternalMetricServiceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(),
        $"SidebarDiagnostics.ExternalMetrics.{Guid.NewGuid():N}");

    [Fact]
    public async Task PreviewReadsNestedNumericValueFromFile()
    {
        var path = await WriteAsync("metric.json", """{"router":{"download":42.5}}""");
        using var service = new ExternalMetricService();

        var snapshot = await service.PreviewAsync(
            Definition(path) with { JsonPath = "router.download", Unit = "Mbps" },
            TestContext.Current.CancellationToken);

        Assert.True(snapshot.IsSuccess);
        Assert.Equal(42.5, snapshot.Value);
        Assert.Equal(42.5, snapshot.Progress);
        Assert.Equal("Mbps", snapshot.Unit);
    }

    [Fact]
    public async Task PreviewRejectsNonNumericValue()
    {
        var path = await WriteAsync("metric.json", """{"value":"offline"}""");
        using var service = new ExternalMetricService();

        var snapshot = await service.PreviewAsync(Definition(path), TestContext.Current.CancellationToken);

        Assert.False(snapshot.IsSuccess);
        Assert.Null(snapshot.Value);
        Assert.Contains("not numeric", snapshot.Status, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewRejectsOversizedFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "large.json");
        await File.WriteAllBytesAsync(
            path,
            new byte[ExternalMetricService.MaximumResponseBytes + 1],
            TestContext.Current.CancellationToken);
        using var service = new ExternalMetricService();

        var snapshot = await service.PreviewAsync(Definition(path), TestContext.Current.CancellationToken);

        Assert.False(snapshot.IsSuccess);
        Assert.Contains("256 KB", snapshot.Status, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReadUsesCachedValueUntilRefreshIntervalExpires()
    {
        var path = await WriteAsync("metric.json", """{"value":10}""");
        var definition = Definition(path);
        using var service = new ExternalMetricService();

        var first = Assert.Single(await service.ReadAsync([definition], TestContext.Current.CancellationToken));
        await File.WriteAllTextAsync(path, """{"value":90}""", TestContext.Current.CancellationToken);
        var second = Assert.Single(await service.ReadAsync([definition], TestContext.Current.CancellationToken));

        Assert.Equal(10, first.Value);
        Assert.Equal(10, second.Value);
    }

    [Fact]
    public async Task PreviewRejectsCredentialsEmbeddedInUrl()
    {
        using var service = new ExternalMetricService();
        var definition = Definition("https://user:secret@example.com/metric.json") with
        {
            SourceKind = ExternalMetricSourceKind.Http
        };

        var snapshot = await service.PreviewAsync(definition, TestContext.Current.CancellationToken);

        Assert.False(snapshot.IsSuccess);
        Assert.Contains("credentials", snapshot.Status, StringComparison.OrdinalIgnoreCase);
    }

    private static ExternalMetricDefinition Definition(string source) => new()
    {
        Id = "metric",
        Title = "Download",
        SourceKind = ExternalMetricSourceKind.File,
        Source = source,
        JsonPath = "value",
        Minimum = 0,
        Maximum = 100,
        RefreshIntervalSeconds = 30
    };

    private async Task<string> WriteAsync(string name, string content)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, name);
        await File.WriteAllTextAsync(path, content, TestContext.Current.CancellationToken);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, true);
        }
    }
}
