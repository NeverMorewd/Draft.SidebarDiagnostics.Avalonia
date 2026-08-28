using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Text.Json;
using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.ExternalMetrics;

public sealed class ExternalMetricService : IExternalMetricService
{
    public const int MaximumResponseBytes = 256 * 1024;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    public ExternalMetricService()
        : this(new HttpClient { Timeout = RequestTimeout })
    {
    }

    public ExternalMetricService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async ValueTask<IReadOnlyList<ExternalMetricSnapshot>> ReadAsync(
        IReadOnlyList<ExternalMetricDefinition> definitions,
        CancellationToken cancellationToken)
    {
        var tasks = definitions
            .Where(definition => definition.IsEnabled)
            .Select(definition => ReadCachedAsync(definition, cancellationToken))
            .ToArray();
        return await Task.WhenAll(tasks);
    }

    public ValueTask<ExternalMetricSnapshot> PreviewAsync(
        ExternalMetricDefinition definition,
        CancellationToken cancellationToken) => ReadCoreAsync(definition, cancellationToken);

    private async Task<ExternalMetricSnapshot> ReadCachedAsync(
        ExternalMetricDefinition definition,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_cache.TryGetValue(definition.Id, out var cached) && cached.NextReadAt > DateTimeOffset.UtcNow)
        {
            return cached.Snapshot;
        }

        var snapshot = await ReadCoreAsync(definition, cancellationToken);
        var interval = snapshot.IsSuccess
            ? TimeSpan.FromSeconds(Math.Clamp(definition.RefreshIntervalSeconds, 5, 3600))
            : TimeSpan.FromSeconds(Math.Clamp(definition.RefreshIntervalSeconds * 2, 10, 300));
        _cache[definition.Id] = new CacheEntry(snapshot, DateTimeOffset.UtcNow + interval);
        return snapshot;
    }

    private async ValueTask<ExternalMetricSnapshot> ReadCoreAsync(
        ExternalMetricDefinition definition,
        CancellationToken cancellationToken)
    {
        try
        {
            Validate(definition);
            var json = definition.SourceKind switch
            {
                ExternalMetricSourceKind.File => await ReadFileAsync(definition.Source, cancellationToken),
                ExternalMetricSourceKind.Http => await ReadHttpAsync(definition.Source, cancellationToken),
                _ => throw new InvalidOperationException("Unsupported source kind.")
            };
            var value = ReadNumber(json, definition.JsonPath);
            var range = definition.Maximum - definition.Minimum;
            var progress = range > 0 ? (value - definition.Minimum) * 100 / range : 0;
            return new ExternalMetricSnapshot(
                definition.Id,
                definition.Title.Trim(),
                value,
                definition.Unit.Trim(),
                Math.Clamp(progress, 0, 100),
                "Live",
                true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ExternalMetricSnapshot(
                definition.Id,
                string.IsNullOrWhiteSpace(definition.Title) ? "External metric" : definition.Title.Trim(),
                null,
                definition.Unit.Trim(),
                0,
                "Source request timed out.",
                false);
        }
        catch (Exception exception) when (exception is IOException
                                           or UnauthorizedAccessException
                                           or HttpRequestException
                                           or JsonException
                                           or InvalidOperationException
                                           or FormatException)
        {
            return new ExternalMetricSnapshot(
                definition.Id,
                string.IsNullOrWhiteSpace(definition.Title) ? "External metric" : definition.Title.Trim(),
                null,
                definition.Unit.Trim(),
                0,
                exception.Message,
                false);
        }
    }

    private async Task<string> ReadHttpAsync(string source, CancellationToken cancellationToken)
    {
        var uri = new Uri(source, UriKind.Absolute);
        if (uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new InvalidOperationException("Only HTTP or HTTPS URLs without embedded credentials are supported.");
        }

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new HttpRequestException($"Source returned HTTP {(int)response.StatusCode}.");
        }

        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
        {
            throw new InvalidOperationException("Response exceeds the 256 KB limit.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await ReadLimitedAsync(stream, cancellationToken);
    }

    private static async Task<string> ReadFileAsync(string source, CancellationToken cancellationToken)
    {
        var file = new FileInfo(source);
        if (!file.Exists) throw new IOException("Source file does not exist.");
        if (file.Length > MaximumResponseBytes) throw new InvalidOperationException("File exceeds the 256 KB limit.");
        await using var stream = file.OpenRead();
        return await ReadLimitedAsync(stream, cancellationToken);
    }

    private static async Task<string> ReadLimitedAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(buffer, cancellationToken);
            if (count == 0) break;
            if (memory.Length + count > MaximumResponseBytes)
            {
                throw new InvalidOperationException("Source exceeds the 256 KB limit.");
            }

            await memory.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }

        return System.Text.Encoding.UTF8.GetString(memory.ToArray());
    }

    private static double ReadNumber(string json, string path)
    {
        using var document = JsonDocument.Parse(json);
        var current = document.RootElement;
        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!current.TryGetProperty(segment, out current))
            {
                throw new JsonException($"JSON path segment '{segment}' was not found.");
            }
        }

        return current.ValueKind switch
        {
            JsonValueKind.Number when current.TryGetDouble(out var value) => value,
            JsonValueKind.String when double.TryParse(current.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) => value,
            _ => throw new JsonException("The selected JSON value is not numeric.")
        };
    }

    private static void Validate(ExternalMetricDefinition definition)
    {
        if (definition.SchemaVersion != 1) throw new InvalidOperationException("Unsupported external metric schema version.");
        if (string.IsNullOrWhiteSpace(definition.Source)) throw new InvalidOperationException("Source is required.");
        if (string.IsNullOrWhiteSpace(definition.JsonPath)) throw new InvalidOperationException("JSON path is required.");
        if (definition.Maximum <= definition.Minimum) throw new InvalidOperationException("Maximum must be greater than minimum.");
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed record CacheEntry(ExternalMetricSnapshot Snapshot, DateTimeOffset NextReadAt);
}
