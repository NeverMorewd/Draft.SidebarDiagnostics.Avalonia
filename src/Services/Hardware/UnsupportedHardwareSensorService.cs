using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class UnsupportedHardwareSensorService(string message) : IHardwareSensorService
{
    public bool IsSupported => false;
    public string CapabilityMessage { get; } = message;

    public ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>([]);
    }

    public void Dispose()
    {
    }
}
