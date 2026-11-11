namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Agents;
using CLIHub.Core.Tests.Fakes;

public class ProbeCacheTests
{
    private static readonly DateTimeOffset StartTime = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    private readonly FakeTimeProvider time = new(StartTime);

    private ProbeCache<int> CreateCache() => new(this.time);

    [Fact]
    public async Task GetOrProbe_WithinTtl_ReturnsCachedValueWithoutInvokingProbe()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe() => Task.FromResult(Interlocked.Increment(ref calls));

        var first = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        this.time.Advance(TimeSpan.FromMinutes(10));
        var second = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);

        Assert.Equal(1, first);
        Assert.Equal(first, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrProbe_AfterTtlExpiry_InvokesProbeAgain()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe() => Task.FromResult(Interlocked.Increment(ref calls));

        var first = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        this.time.Advance(TimeSpan.FromMinutes(20));
        var second = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrProbe_ConcurrentCallsForOneKey_InvokeProbeOnceAndShareResult()
    {
        var cache = this.CreateCache();
        var gate = new TaskCompletionSource();
        var calls = 0;
        async Task<int> probe()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return 42;
        }

        var first = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        var second = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.Equal(42, results[0]);
        Assert.Equal(results[0], results[1]);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrProbe_OneCallerCancels_SurfacesCancellationWithoutCancellingSharedProbe()
    {
        var cache = this.CreateCache();
        var gate = new TaskCompletionSource();
        var calls = 0;
        async Task<int> probe()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return 42;
        }

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        var first = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe, cancelled.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);

        var second = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        gate.SetResult();

        Assert.Equal(42, await second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task GetOrProbe_AfterRemove_ProbesAgainInsteadOfReusingCompletedInFlightTask()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe() => Task.FromResult(Interlocked.Increment(ref calls));

        var first = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        cache.Remove("key");
        var second = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetOrProbe_FaultedProbe_RemovesInFlightEntrySoNextCallProbesAgain()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe()
        {
            Interlocked.Increment(ref calls);
            throw new InvalidOperationException("boom");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe));
        Assert.Equal(1, calls);

        var second = await cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), () => Task.FromResult(42));

        Assert.Equal(42, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task ClearAndRemove_DuringInFlightProbe_LeaveTheSharedProbeInFlight()
    {
        var cache = this.CreateCache();
        var gate = new TaskCompletionSource();
        var calls = 0;
        async Task<int> probe()
        {
            Interlocked.Increment(ref calls);
            await gate.Task;
            return 42;
        }

        var first = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        cache.Clear();
        cache.Remove("key");

        var second = cache.GetOrProbeAsync("key", TimeSpan.FromMinutes(15), probe);
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, value => Assert.Equal(42, value));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Clear_CompletedEntries_RemovesEveryKey()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe() => Task.FromResult(Interlocked.Increment(ref calls));

        await cache.GetOrProbeAsync("a", TimeSpan.FromMinutes(15), probe);
        await cache.GetOrProbeAsync("b", TimeSpan.FromMinutes(15), probe);

        cache.Clear();

        await cache.GetOrProbeAsync("a", TimeSpan.FromMinutes(15), probe);
        await cache.GetOrProbeAsync("b", TimeSpan.FromMinutes(15), probe);

        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task Remove_OneKey_LeavesOtherKeysCached()
    {
        var cache = this.CreateCache();
        var calls = 0;
        Task<int> probe() => Task.FromResult(Interlocked.Increment(ref calls));

        await cache.GetOrProbeAsync("a", TimeSpan.FromMinutes(15), probe);
        await cache.GetOrProbeAsync("b", TimeSpan.FromMinutes(15), probe);

        cache.Remove("a");

        await cache.GetOrProbeAsync("a", TimeSpan.FromMinutes(15), probe);
        await cache.GetOrProbeAsync("b", TimeSpan.FromMinutes(15), probe);

        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task GetOrProbe_HeldCallerContinuationAfterNewerProbePublished_DoesNotEvictNewerInFlightProbe()
    {
        const string key = "key";
        var cache = this.CreateCache();
        var context = new QueueingSynchronizationContext();
        var firstResult = new TaskCompletionSource<int>();
        var secondResult = new TaskCompletionSource<int>();
        var calls = 0;

        // Non-async delegates: any await inside the probe would capture the queueing context
        // installed below and deadlock the test (design.md A3).
        Task<int> firstProbe()
        {
            Interlocked.Increment(ref calls);
            return firstResult.Task;
        }

        Task<int> secondProbe()
        {
            Interlocked.Increment(ref calls);
            return secondResult.Task;
        }

        // Caller A: the continuation after WaitAsync is posted to the queueing context and held.
        Task<int> a;
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            a = cache.GetOrProbeAsync(key, TimeSpan.FromMinutes(15), firstProbe);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        // Caller B joins the same probe; its finally removes L1 when it completes.
        var b = cache.GetOrProbeAsync(key, TimeSpan.FromMinutes(15), firstProbe);
        firstResult.SetResult(1);
        Assert.Equal(1, await b);

        // `await b` proves the shared probe completed but not that A's Post has run yet: the await
        // dispatches A's continuation through the queueing context, and when that dispatch reaches
        // the queue is a runtime scheduling detail. Wait for it (bounded, so a broken hold fails
        // loudly instead of hanging).
        await context.FirstPost.WaitAsync(TimeSpan.FromSeconds(5));

        // A is provably still held.
        Assert.Equal(1, context.PendingCount);
        Assert.False(a.IsCompleted);

        // Invalidate the completed entry and let caller C publish a newer, still-running probe.
        cache.Remove(key);
        var c = cache.GetOrProbeAsync(key, TimeSpan.FromMinutes(15), secondProbe);
        Assert.Equal(2, calls);

        // Release A's delayed continuation: it must not remove C's probe.
        Assert.Equal(1, context.RunPending());
        Assert.True(a.IsCompleted);

        // D must join C's probe. On the key-based code D starts a third probe and calls is 3.
        var d = cache.GetOrProbeAsync(key, TimeSpan.FromMinutes(15), secondProbe);
        Assert.Equal(2, calls);

        secondResult.SetResult(2);
        var results = await Task.WhenAll(c, d);

        Assert.Equal(1, await a);
        Assert.All(results, value => Assert.Equal(2, value));
        Assert.Equal(2, calls);
    }
}
