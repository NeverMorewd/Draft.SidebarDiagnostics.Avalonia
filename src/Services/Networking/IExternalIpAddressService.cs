namespace SidebarDiagnostics.App.Services.Networking;

public interface IExternalIpAddressService : IDisposable
{
    ValueTask<string?> GetAddressAsync(CancellationToken cancellationToken);
}
