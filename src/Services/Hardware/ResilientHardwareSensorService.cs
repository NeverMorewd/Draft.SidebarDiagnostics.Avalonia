using System.Diagnostics;
using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Diagnostics;

namespace SidebarDiagnostics.App.Services.Hardware;

public sealed class ResilientHardwareSensorService : IHardwareSensorService
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private readonly IHardwareSensorService inner;
    private readonly TimeSpan timeout;
    private readonly CancellationTokenSource lifetimeCancellation = new();
    private readonly object syncRoot = new();
    private ActiveRead? activeRead;
    private IReadOnlyList<HardwareSensorReading> lastSuccessfulReadings = [];
    private string status;
    private bool isDisposed;

    public ResilientHardwareSensorService(IHardwareSensorService inner, TimeSpan? timeout = null)
    {
        this.inner = inner;
        this.timeout = timeout ?? DefaultTimeout;
        if (this.timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        status = inner.CapabilityMessage;
    }

    public bool IsSupported => inner.IsSupported;
    public string CapabilityMessage => status;

    public async ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ActiveRead read;
        lock (syncRoot)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);
            activeRead ??= StartRead();
            read = activeRead;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var readings = await read.Task.WaitAsync(timeout, cancellationToken);
            lock (syncRoot)
            {
                Complete(read);
                lastSuccessfulReadings = readings;
                status = readings.Count > 0 ? $"{readings.Count} hardware sensors" : inner.CapabilityMessage;
            }

            return readings;
        }
        catch (TimeoutException exception)
        {
            Cancel(read);
            SafeDiagnosticLog.Write("HardwarePolling", "Timeout", exception, stopwatch.ElapsedMilliseconds);
            lock (syncRoot)
            {
                status = $"Hardware polling exceeded {timeout.TotalSeconds:0.#} seconds; showing the last successful sample.";
                return lastSuccessfulReadings;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException exception)
        {
            SafeDiagnosticLog.Write("HardwarePolling", "ProviderCanceled", exception, stopwatch.ElapsedMilliseconds);
            lock (syncRoot)
            {
                Complete(read);
                status = "Hardware polling was canceled; showing the last successful sample.";
                return lastSuccessfulReadings;
            }
        }
        catch (Exception exception)
        {
            SafeDiagnosticLog.Write("HardwarePolling", "ProviderFailure", exception, stopwatch.ElapsedMilliseconds);
            lock (syncRoot)
            {
                Complete(read);
                status = $"Hardware polling failed ({exception.GetType().Name}); showing the last successful sample.";
                return lastSuccessfulReadings;
            }
        }
    }

    private ActiveRead StartRead()
    {
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token);
        var task = Task.Run(async () => await inner.ReadAsync(cancellation.Token), CancellationToken.None);
        return new ActiveRead(task, cancellation);
    }

    private void Complete(ActiveRead read)
    {
        if (!ReferenceEquals(activeRead, read))
        {
            return;
        }

        activeRead = null;
        read.Cancellation.Dispose();
    }

    private void Cancel(ActiveRead read)
    {
        lock (syncRoot)
        {
            if (!ReferenceEquals(activeRead, read))
            {
                return;
            }

            try
            {
                read.Cancellation.Cancel();
            }
            catch (AggregateException exception)
            {
                SafeDiagnosticLog.Write("HardwarePolling", "CancellationCallbackFailure", exception);
            }
        }
    }

    public void Dispose()
    {
        ActiveRead? read;
        lock (syncRoot)
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            try
            {
                lifetimeCancellation.Cancel();
            }
            catch (AggregateException exception)
            {
                SafeDiagnosticLog.Write("HardwarePolling", "DisposeCancellationFailure", exception);
            }
            read = activeRead;
            activeRead = null;
        }

        if (read is null || read.Task.IsCompleted)
        {
            read?.Cancellation.Dispose();
            inner.Dispose();
            lifetimeCancellation.Dispose();
            return;
        }

        _ = read.Task.ContinueWith(
            completed =>
            {
                _ = completed.Exception;
                read.Cancellation.Dispose();
                inner.Dispose();
                lifetimeCancellation.Dispose();
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private sealed record ActiveRead(Task<IReadOnlyList<HardwareSensorReading>> Task, CancellationTokenSource Cancellation);
}
