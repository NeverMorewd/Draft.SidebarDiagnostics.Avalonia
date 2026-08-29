using System.IO;
using SidebarDiagnostics.App.Services.Diagnostics;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class DiagnosticDeviceSelectionTests
{
    [Theory]
    [InlineData("C:\\", "NTFS", DriveType.Fixed, true, true, true)]
    [InlineData("/", "overlay", DriveType.Fixed, true, false, true)]
    [InlineData("/mnt/c", "9p", DriveType.Fixed, true, false, true)]
    [InlineData("/media/user/backup", "ext4", DriveType.Fixed, true, false, true)]
    [InlineData("/Volumes/External", "apfs", DriveType.Fixed, true, false, true)]
    [InlineData("/proc", "proc", DriveType.Ram, true, false, false)]
    [InlineData("/run", "tmpfs", DriveType.Ram, true, false, false)]
    [InlineData("/mnt/wsl", "tmpfs", DriveType.Fixed, true, false, false)]
    [InlineData("/System/Volumes/Data", "apfs", DriveType.Fixed, true, false, false)]
    [InlineData("/etc/hosts", "9p", DriveType.Fixed, false, false, false)]
    public void DriveSelectionKeepsUserVolumesAndRejectsSystemMounts(
        string name,
        string format,
        DriveType driveType,
        bool isDirectory,
        bool isWindows,
        bool expected)
    {
        Assert.Equal(expected, DiagnosticDeviceSelection.ShouldIncludeDrive(name, format, driveType, isDirectory, isWindows));
    }

    [Fact]
    public void NetworkSelectionPrefersTheInterfaceOwningTheDefaultRouteAddress()
    {
        var candidates = new[]
        {
            new Candidate("Hyper-V", false, false, 3, 10_000),
            new Candidate("Wi-Fi", true, true, 2, 1_000),
            new Candidate("Ethernet", false, true, 3, 2_500)
        };

        var selected = DiagnosticDeviceSelection.SelectPrimary(
            candidates,
            candidate => candidate.OwnsPreferredAddress,
            candidate => candidate.HasDefaultGateway,
            candidate => candidate.TypePriority,
            candidate => candidate.Speed,
            candidate => candidate.Name);

        Assert.Equal("Wi-Fi", selected?.Name);
    }

    [Fact]
    public void NetworkSelectionFallsBackToGatewayThenPhysicalPriority()
    {
        var candidates = new[]
        {
            new Candidate("Virtual", false, false, 3, 10_000),
            new Candidate("Wi-Fi", false, true, 2, 1_000),
            new Candidate("Ethernet", false, true, 3, 100)
        };

        var selected = DiagnosticDeviceSelection.SelectPrimary(
            candidates,
            candidate => candidate.OwnsPreferredAddress,
            candidate => candidate.HasDefaultGateway,
            candidate => candidate.TypePriority,
            candidate => candidate.Speed,
            candidate => candidate.Name);

        Assert.Equal("Ethernet", selected?.Name);
    }

    private sealed record Candidate(string Name, bool OwnsPreferredAddress, bool HasDefaultGateway, int TypePriority, long Speed);
}
