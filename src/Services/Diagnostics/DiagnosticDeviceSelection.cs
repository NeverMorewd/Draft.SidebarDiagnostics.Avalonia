using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SidebarDiagnostics.App.Services.Diagnostics;

internal static class DiagnosticDeviceSelection
{
    private static readonly string[] UserMountPrefixes = ["/mnt/", "/media/", "/run/media/", "/Volumes/", "/home/", "/data/", "/srv/"];
    private static readonly string[] InternalMountPrefixes = ["/mnt/wsl/", "/mnt/wslg/", "/System/", "/private/", "/boot/", "/snap/", "/var/lib/docker/", "/var/lib/containers/"];
    private static readonly HashSet<string> PseudoFileSystems = new(StringComparer.OrdinalIgnoreCase)
    {
        "autofs", "binfmt_misc", "cgroup", "cgroup2", "configfs", "debugfs", "devpts", "devtmpfs",
        "fusectl", "hugetlbfs", "mqueue", "nsfs", "overlay", "proc", "procfs", "pstore", "ramfs",
        "securityfs", "squashfs", "sysfs", "tmpfs", "tracefs"
    };

    public static IReadOnlyList<NetworkInterface> SelectPrimaryNetworks(IEnumerable<NetworkInterface> networks)
    {
        var preferredAddress = ReadPreferredLocalAddress();
        var candidates = networks
            .Select(TryCreateCandidate)
            .Where(candidate => candidate is not null)
            .Select(candidate => candidate!)
            .ToArray();
        var selected = SelectPrimary(
            candidates,
            candidate => preferredAddress is not null && candidate.Addresses.Contains(preferredAddress),
            candidate => candidate.HasDefaultGateway,
            candidate => TypePriority(candidate.Network.NetworkInterfaceType),
            candidate => candidate.Network.Speed,
            candidate => candidate.Network.Name);
        return selected is null ? [] : [selected.Network];
    }

    public static bool ShouldIncludeDrive(
        string name,
        string format,
        DriveType driveType,
        bool isDirectory,
        bool isWindows)
    {
        if (!isDirectory)
        {
            return false;
        }

        if (isWindows)
        {
            return name.Length >= 2
                && char.IsAsciiLetter(name[0])
                && name[1] == ':'
                && driveType is DriveType.Fixed or DriveType.Removable or DriveType.Network or DriveType.CDRom;
        }

        var normalized = name.Replace('\\', '/').TrimEnd('/');
        if (normalized.Length == 0)
        {
            return true;
        }

        var path = $"{normalized}/";
        if (InternalMountPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return false;
        }

        if (PseudoFileSystems.Contains(format))
        {
            return false;
        }

        return driveType == DriveType.Network
            || UserMountPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
    }

    internal static T? SelectPrimary<T>(
        IEnumerable<T> candidates,
        Func<T, bool> ownsPreferredAddress,
        Func<T, bool> hasDefaultGateway,
        Func<T, int> typePriority,
        Func<T, long> speed,
        Func<T, string> name) => candidates
            .OrderByDescending(ownsPreferredAddress)
            .ThenByDescending(hasDefaultGateway)
            .ThenByDescending(typePriority)
            .ThenByDescending(speed)
            .ThenBy(name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

    private static NetworkCandidate? TryCreateCandidate(NetworkInterface network)
    {
        if (network.OperationalStatus != OperationalStatus.Up
            || network.IsReceiveOnly
            || network.Speed <= 0
            || network.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel or NetworkInterfaceType.Unknown)
        {
            return null;
        }

        try
        {
            var properties = network.GetIPProperties();
            var addresses = properties.UnicastAddresses
                .Select(address => address.Address)
                .Where(address => !IPAddress.IsLoopback(address))
                .ToHashSet();
            if (addresses.Count == 0)
            {
                return null;
            }

            var hasGateway = properties.GatewayAddresses.Any(gateway =>
                !gateway.Address.Equals(IPAddress.Any)
                && !gateway.Address.Equals(IPAddress.IPv6Any));
            return new NetworkCandidate(network, addresses, hasGateway);
        }
        catch (NetworkInformationException)
        {
            return null;
        }
    }

    private static IPAddress? ReadPreferredLocalAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect(new IPEndPoint(IPAddress.Parse("192.0.2.1"), 9));
            return (socket.LocalEndPoint as IPEndPoint)?.Address;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static int TypePriority(NetworkInterfaceType type) => type switch
    {
        NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT => 3,
        NetworkInterfaceType.Wireless80211 => 2,
        NetworkInterfaceType.Ppp => 1,
        _ => 0
    };

    private sealed record NetworkCandidate(NetworkInterface Network, HashSet<IPAddress> Addresses, bool HasDefaultGateway);
}
