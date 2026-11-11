namespace CLIHub.Core.Agents;

using System.Collections.Concurrent;

/// <summary>
///   A per-key time-to-live cache for asynchronous probes that coalesces concurrent cache
///   misses into one shared probe and removes the in-flight entry only once that probe has
///   completed.
/// </summary>
/// <typeparam name="TValue"> The value produced by a probe. </typeparam>
internal sealed class ProbeCache<TValue>
{
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> entries = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<TValue>>> inFlight = new();

    /// <summary>
    ///   Creates the cache with the supplied time source.
    /// </summary>
    /// <param name="timeProvider"> The time source used to timestamp entries and decide expiry. </param>
    internal ProbeCache(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        this.timeProvider = timeProvider;
    }

    /// <summary>
    ///   Returns the cached value for <paramref name="key"/> when its age is within
    ///   <paramref name="ttl"/>; otherwise runs <paramref name="probe"/>, coalescing
    ///   concurrent misses into one shared probe.
    /// </summary>
    /// <remarks>
    ///   The probe always runs to completion independently of any single caller: a caller's
    ///   <paramref name="cancellationToken"/> only cancels that caller's wait, never the shared
    ///   probe. The in-flight entry is removed only after the probe's task has completed, and only
    ///   the entry that caller awaited is removed, so a faulting probe cannot permanently poison
    ///   the key.
    /// </remarks>
    /// <param name="key"> The cache key. </param>
    /// <param name="ttl"> The time-to-live used to decide whether a cached entry is still fresh. </param>
    /// <param name="probe"> Produces the value on a cache miss. </param>
    /// <param name="cancellationToken"> Cancels this caller's wait, not the shared probe. </param>
    /// <returns> The cached or freshly probed value. </returns>
    internal async Task<TValue> GetOrProbeAsync(string key, TimeSpan ttl, Func<Task<TValue>> probe, CancellationToken cancellationToken = default)
    {
        var now = this.timeProvider.GetUtcNow();
        if (this.entries.TryGetValue(key, out var cached) && now - cached.Timestamp < ttl)
        {
            return cached.Value;
        }

        var lazy = this.inFlight.GetOrAdd(
            key,
            _ => new Lazy<Task<TValue>>(
                () => this.ProbeAndCacheAsync(key, probe),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                this.inFlight.TryRemove(new KeyValuePair<string, Lazy<Task<TValue>>>(key, lazy));
            }
        }
    }

    /// <summary>
    ///   Removes every completed cache entry. The in-flight map is deliberately left alone so
    ///   concurrent callers keep sharing one probe.
    /// </summary>
    internal void Clear() => this.entries.Clear();

    /// <summary>
    ///   Removes the completed cache entry for <paramref name="key"/>, if any. The in-flight
    ///   map is deliberately left alone so concurrent callers keep sharing one probe; a probe
    ///   that started before this call may still publish its result.
    /// </summary>
    /// <param name="key"> The cache key. </param>
    internal void Remove(string key) => this.entries.TryRemove(key, out _);

    /// <summary>
    ///   Runs the probe and caches its result with a timestamp taken after the probe returns.
    ///   Kept <c>async</c> so a faulting probe is captured in the returned task rather than
    ///   thrown from the Lazy factory, which would leave the in-flight entry permanently poisoned.
    /// </summary>
    private async Task<TValue> ProbeAndCacheAsync(string key, Func<Task<TValue>> probe)
    {
        var value = await probe().ConfigureAwait(false);
        this.entries[key] = new CacheEntry(value, this.timeProvider.GetUtcNow());
        return value;
    }

    private readonly record struct CacheEntry(TValue Value, DateTimeOffset Timestamp);
}
