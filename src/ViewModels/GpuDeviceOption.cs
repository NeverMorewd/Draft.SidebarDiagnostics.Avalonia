using SidebarDiagnostics.App.Models;

namespace SidebarDiagnostics.App.ViewModels;

public sealed record GpuDeviceOption(string DeviceId, string Name, HardwareVendor Vendor)
{
    public string DisplayName => Vendor == HardwareVendor.Unknown ? Name : $"{Name} · {Vendor}";
}
