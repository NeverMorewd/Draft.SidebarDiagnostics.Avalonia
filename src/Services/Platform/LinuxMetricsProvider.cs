using System.Globalization;

namespace SidebarDiagnostics.App.Services.Platform;

public sealed class LinuxMetricsProvider : IPlatformMetricsProvider
{
    private const string CpuStatPath = "/proc/stat";
    private const string MemoryInfoPath = "/proc/meminfo";
    private readonly LinuxCpuUsageTracker cpuUsageTracker = new();

    public async ValueTask<PlatformMetrics> SampleAsync(CancellationToken cancellationToken)
    {
        var cpuLine = await ReadFirstLineAsync(CpuStatPath, cancellationToken);
        var memoryLines = await File.ReadAllLinesAsync(MemoryInfoPath, cancellationToken);
        var (idle, total) = ParseCpuLine(cpuLine);
        var (usedMemory, totalMemory) = ParseMemory(memoryLines);
        var cpuUsage = cpuUsageTracker.Update(idle, total);
        return new PlatformMetrics(Math.Clamp(cpuUsage, 0, 100), usedMemory, totalMemory);
    }

    private static async Task<string> ReadFirstLineAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream);
        return await reader.ReadLineAsync(cancellationToken) ?? throw new InvalidDataException($"{path} is empty.");
    }

    private static (ulong Idle, ulong Total) ParseCpuLine(string line)
    {
        var values = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)
            .Select(value => ulong.Parse(value, CultureInfo.InvariantCulture))
            .ToArray();

        if (values.Length < 4) throw new InvalidDataException("The Linux CPU statistics are incomplete.");
        var idle = values[3] + (values.Length > 4 ? values[4] : 0);
        return (idle, values.Aggregate(0UL, (total, value) => total + value));
    }

    internal static (long Used, long Total) ParseMemory(IEnumerable<string> lines)
    {
        var values = lines
            .Select(line => line.Split(':', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => ParseKilobytes(parts[1]), StringComparer.Ordinal);

        var total = values.GetValueOrDefault("MemTotal");
        if (total <= 0)
        {
            throw new InvalidDataException("The Linux memory statistics do not include MemTotal.");
        }

        var available = values.TryGetValue("MemAvailable", out var reportedAvailable)
            ? reportedAvailable
            : values.GetValueOrDefault("MemFree")
              + values.GetValueOrDefault("Buffers")
              + values.GetValueOrDefault("Cached")
              + values.GetValueOrDefault("SReclaimable")
              - values.GetValueOrDefault("Shmem");
        available = Math.Clamp(available, 0, total);
        return (total - available, total);
    }

    private static long ParseKilobytes(string value)
    {
        var number = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return checked(long.Parse(number, CultureInfo.InvariantCulture) * 1024);
    }
}
