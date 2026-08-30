using System.Net;
using SidebarDiagnostics.App.Services.Networking;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class ExternalIpAddressServiceTests
{
    [Fact]
    public async Task GetAddressAsyncReturnsValidatedAddressAndUsesCache()
    {
        var handler = new StubHttpMessageHandler(" 203.0.113.42 ");
        using var service = new ExternalIpAddressService(new HttpClient(handler));

        var first = await service.GetAddressAsync(CancellationToken.None);
        var second = await service.GetAddressAsync(CancellationToken.None);

        Assert.Equal("203.0.113.42", first);
        Assert.Equal(first, second);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task GetAddressAsyncRejectsUnexpectedResponse()
    {
        using var service = new ExternalIpAddressService(new HttpClient(new StubHttpMessageHandler("not an address")));

        var address = await service.GetAddressAsync(CancellationToken.None);

        Assert.Null(address);
    }

    private sealed class StubHttpMessageHandler(string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });
        }
    }
}
