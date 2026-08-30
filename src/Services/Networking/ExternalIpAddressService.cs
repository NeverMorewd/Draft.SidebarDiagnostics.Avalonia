using System.Net;
using System.Net.Http.Headers;

namespace SidebarDiagnostics.App.Services.Networking;

public sealed class ExternalIpAddressService : IExternalIpAddressService
{
    private static readonly Uri Endpoint = new("https://api.ipify.org/");
    private static readonly TimeSpan SuccessLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan FailureLifetime = TimeSpan.FromMinutes(5);
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private string? _cachedAddress;
    private DateTimeOffset _nextRefreshAt;

    public ExternalIpAddressService()
        : this(CreateHttpClient())
    {
    }

    internal ExternalIpAddressService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async ValueTask<string?> GetAddressAsync(CancellationToken cancellationToken)
    {
        if (DateTimeOffset.UtcNow < _nextRefreshAt)
        {
            return _cachedAddress;
        }

        await _refreshGate.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow < _nextRefreshAt)
            {
                return _cachedAddress;
            }

            try
            {
                var response = await _httpClient.GetStringAsync(Endpoint, cancellationToken);
                _cachedAddress = IPAddress.TryParse(response.Trim(), out var address)
                    ? address.ToString()
                    : null;
                _nextRefreshAt = DateTimeOffset.UtcNow + (_cachedAddress is null ? FailureLifetime : SuccessLifetime);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
            {
                _nextRefreshAt = DateTimeOffset.UtcNow + FailureLifetime;
            }

            return _cachedAddress;
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SidebarDiagnostics", "1.0"));
        return client;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _refreshGate.Dispose();
    }
}
