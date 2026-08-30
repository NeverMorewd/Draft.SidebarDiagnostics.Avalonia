namespace SidebarDiagnostics.App.Services.Platform;

internal sealed class LinuxCpuUsageTracker
{
    private ulong previousIdle;
    private ulong previousTotal;
    private bool hasPreviousSample;

    public double Update(ulong idle, ulong total)
    {
        var usage = 0d;
        if (hasPreviousSample && total > previousTotal && idle >= previousIdle)
        {
            var totalDelta = total - previousTotal;
            var idleDelta = idle - previousIdle;
            if (idleDelta <= totalDelta)
            {
                usage = (totalDelta - idleDelta) * 100d / totalDelta;
            }
        }

        previousIdle = idle;
        previousTotal = total;
        hasPreviousSample = true;
        return usage;
    }
}
