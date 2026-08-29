namespace SidebarDiagnostics.App.Services.Platform;

internal static class CpuTickUsage
{
    public static double Calculate(
        uint previousUser,
        uint previousSystem,
        uint previousIdle,
        uint previousNice,
        uint currentUser,
        uint currentSystem,
        uint currentIdle,
        uint currentNice)
    {
        var user = Delta(currentUser, previousUser);
        var system = Delta(currentSystem, previousSystem);
        var idle = Delta(currentIdle, previousIdle);
        var nice = Delta(currentNice, previousNice);
        var total = user + system + idle + nice;
        return total == 0 ? 0 : (user + system + nice) * 100d / total;
    }

    private static ulong Delta(uint current, uint previous) =>
        current >= previous ? current - previous : (ulong)uint.MaxValue - previous + current + 1;
}
