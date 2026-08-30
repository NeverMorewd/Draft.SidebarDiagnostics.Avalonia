namespace SidebarDiagnostics.App.Services;

internal readonly record struct NetworkTrafficSample(string InterfaceId, long ReceivedBytes, long SentBytes);
internal readonly record struct NetworkTrafficRate(double DownloadBytesPerSecond, double UploadBytesPerSecond);

internal sealed class NetworkTrafficRateTracker
{
    private string? interfaceId;
    private DateTimeOffset sampledAt;
    private long receivedBytes;
    private long sentBytes;

    public NetworkTrafficRate Update(NetworkTrafficSample? sample, DateTimeOffset now)
    {
        if (sample is null)
        {
            interfaceId = null;
            return default;
        }

        var current = sample.Value;
        if (!string.Equals(interfaceId, current.InterfaceId, StringComparison.Ordinal)
            || current.ReceivedBytes < receivedBytes
            || current.SentBytes < sentBytes)
        {
            Reset(current, now);
            return default;
        }

        var elapsedSeconds = (now - sampledAt).TotalSeconds;
        if (elapsedSeconds <= 0)
        {
            Reset(current, now);
            return default;
        }

        var rate = new NetworkTrafficRate(
            (current.ReceivedBytes - receivedBytes) / elapsedSeconds,
            (current.SentBytes - sentBytes) / elapsedSeconds);
        Reset(current, now);
        return rate;
    }

    private void Reset(NetworkTrafficSample sample, DateTimeOffset now)
    {
        interfaceId = sample.InterfaceId;
        receivedBytes = sample.ReceivedBytes;
        sentBytes = sample.SentBytes;
        sampledAt = now;
    }
}
