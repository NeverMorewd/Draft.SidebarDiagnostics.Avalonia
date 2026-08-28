using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public interface IHardwareSensorService : IDisposable
{
    bool IsSupported { get; }
    string CapabilityMessage { get; }
    ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken);
}
