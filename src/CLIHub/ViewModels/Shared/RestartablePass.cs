namespace CLIHub.ViewModels;

/// <summary>
///   Manages a restartable pass with cancellation-token lifecycle and generation-based staleness detection.
/// </summary>
internal sealed class RestartablePass : IDisposable
{
    private CancellationTokenSource? cts;
    private int generation;

    /// <summary>
    ///   Begins a new pass, invalidating any in-flight pass.
    /// </summary>
    /// <returns> A lease containing the current generation and refresh token. </returns>
    public PassLease Begin()
    {
        this.cts?.Cancel();
        this.cts?.Dispose();
        this.cts = new CancellationTokenSource();
        var gen = ++this.generation;
        return new PassLease(gen, this.cts.Token);
    }

    /// <summary>
    ///   Checks whether a pass lease is still current and uncancelled.
    /// </summary>
    /// <param name="generation"> The generation from the lease. </param>
    /// <param name="token"> The lease or linked cancellation token. </param>
    /// <returns> True when the pass is current. </returns>
    public bool IsCurrent(int generation, CancellationToken token) =>
        !token.IsCancellationRequested && generation == this.generation;

    /// <summary>
    ///   Invalidates and disposes the current pass.
    /// </summary>
    public void Dispose()
    {
        this.generation++;
        this.cts?.Cancel();
        this.cts?.Dispose();
        this.cts = null;
    }
}

/// <summary>
///   A lease returned by <see cref="RestartablePass.Begin" />.
/// </summary>
/// <param name="Generation"> The pass generation. </param>
/// <param name="RefreshToken"> The refresh-scoped cancellation token. </param>
internal readonly record struct PassLease(int Generation, CancellationToken RefreshToken);
