namespace SidebarDiagnostics.App.Services;

internal readonly record struct NetworkTrafficSample(string InterfaceId, long ReceivedBytes, long SentBytes, long LinkSpeedBitsPerSecond = 0);
internal readonly record struct NetworkTrafficRate(double DownloadBytesPerSecond, double UploadBytesPerSecond);

internal sealed class NetworkTrafficRateTracker
{
    internal static double CalculateActivityPercent(NetworkTrafficRate rate, long linkSpeedBitsPerSecond) =>
        linkSpeedBitsPerSecond <= 0 ? 0 : Math.Clamp(
            Math.Max(rate.DownloadBytesPerSecond, rate.UploadBytesPerSecond) * 8 / linkSpeedBitsPerSecond * 100, 0, 100);

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
