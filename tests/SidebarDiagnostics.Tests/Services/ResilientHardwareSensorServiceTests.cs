using SidebarDiagnostics.App.Models;
using SidebarDiagnostics.App.Services.Hardware;
using Xunit;

namespace SidebarDiagnostics.Tests.Services;

public sealed class ResilientHardwareSensorServiceTests
{
    [Fact]
    public async Task TimeoutReturnsLastSuccessfulSampleWithoutStartingConcurrentPolls()
    {
        var reading = CreateReading(42);
        var stalled = new TaskCompletionSource<IReadOnlyList<HardwareSensorReading>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new StubHardwareSensorService((call, _) => call == 1
            ? ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>([reading])
            : new ValueTask<IReadOnlyList<HardwareSensorReading>>(stalled.Task));
        using var service = new ResilientHardwareSensorService(provider, TimeSpan.FromMilliseconds(100));

        var first = await service.ReadAsync(TestContext.Current.CancellationToken);
        var second = await service.ReadAsync(TestContext.Current.CancellationToken);
        var third = await service.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Same(reading, Assert.Single(first));
        Assert.Same(reading, Assert.Single(second));
        Assert.Same(reading, Assert.Single(third));
        Assert.Equal(2, provider.CallCount);
        Assert.Contains("exceeded", service.CapabilityMessage, StringComparison.Ordinal);
        stalled.SetResult([CreateReading(43)]);
    }

    [Fact]
    public async Task ProviderFailureIsContainedAndTheNextPollCanRecover()
    {
        var provider = new StubHardwareSensorService((call, _) => call switch
        {
            1 => ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>([CreateReading(10)]),
            2 => ValueTask.FromException<IReadOnlyList<HardwareSensorReading>>(new InvalidOperationException("Sensitive details")),
            _ => ValueTask.FromResult<IReadOnlyList<HardwareSensorReading>>([CreateReading(20)])
        });
        using var service = new ResilientHardwareSensorService(provider, TimeSpan.FromSeconds(1));

        var first = await service.ReadAsync(TestContext.Current.CancellationToken);
        var failed = await service.ReadAsync(TestContext.Current.CancellationToken);
        var recovered = await service.ReadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(10, Assert.Single(first).Value);
        Assert.Equal(10, Assert.Single(failed).Value);
        Assert.Equal(20, Assert.Single(recovered).Value);
        Assert.DoesNotContain("Sensitive details", service.CapabilityMessage, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallerCancellationIsPropagated()
    {
        var provider = new StubHardwareSensorService((_, cancellationToken) =>
            new ValueTask<IReadOnlyList<HardwareSensorReading>>(Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken)
                .ContinueWith<IReadOnlyList<HardwareSensorReading>>(_ => [], CancellationToken.None)));
        using var service = new ResilientHardwareSensorService(provider, TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await service.ReadAsync(cancellation.Token));
    }

    [Fact]
    public async Task DisposeCancelsAnActivePollAndDisposesTheProvider()
    {
        var provider = new StubHardwareSensorService((_, cancellationToken) =>
            new ValueTask<IReadOnlyList<HardwareSensorReading>>(WaitForCancellationAsync(cancellationToken)));
        var service = new ResilientHardwareSensorService(provider, TimeSpan.FromSeconds(5));

        var read = service.ReadAsync(CancellationToken.None).AsTask();
        await Task.Delay(25, TestContext.Current.CancellationToken);
        service.Dispose();

        Assert.Empty(await read);
        await provider.Disposed.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
    }

    private static async Task<IReadOnlyList<HardwareSensorReading>> WaitForCancellationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return [];
    }

    private static HardwareSensorReading CreateReading(double value) => new(
        "test:cpu:load",
        "test:cpu",
        "Test CPU",
        HardwareDeviceType.Cpu,
        HardwareVendor.Unknown,
        "Load",
        HardwareSensorType.Load,
        value,
        "%");

    private sealed class StubHardwareSensorService(
        Func<int, CancellationToken, ValueTask<IReadOnlyList<HardwareSensorReading>>> read) : IHardwareSensorService
    {
        private int callCount;
        private int isDisposed;
        private readonly TaskCompletionSource disposed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int CallCount => Volatile.Read(ref callCount);
        public bool IsDisposed => Volatile.Read(ref isDisposed) != 0;
        public Task Disposed => disposed.Task;
        public bool IsSupported => true;
        public string CapabilityMessage => "Test provider";

        public ValueTask<IReadOnlyList<HardwareSensorReading>> ReadAsync(CancellationToken cancellationToken) =>
            read(Interlocked.Increment(ref callCount), cancellationToken);

        public void Dispose()
        {
            Interlocked.Exchange(ref isDisposed, 1);
            disposed.TrySetResult();
        }
    }
}
