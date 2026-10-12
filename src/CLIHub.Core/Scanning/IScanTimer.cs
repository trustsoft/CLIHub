namespace CLIHub.Core.Scanning;

/// <summary>
///   A one-shot timer used to debounce watcher events.
/// </summary>
public interface IScanTimer : IDisposable
{
    /// <summary>
    ///   Raised once after the armed delay elapses.
    /// </summary>
    event EventHandler? Elapsed;

    /// <summary>
    ///   Schedules the timer, resetting any pending delay.
    /// </summary>
    /// <param name="delay"> The delay before <see cref="Elapsed"/> is raised. </param>
    void Arm(TimeSpan delay);

    /// <summary>
    ///   Cancels a pending delay.
    /// </summary>
    void Cancel();
}

/// <summary>
///   Creates debounce timers.
/// </summary>
public interface IScanTimerFactory
{
    /// <summary>
    ///   Creates a new, disarmed timer.
    /// </summary>
    /// <returns> The created timer. </returns>
    IScanTimer Create();
}

/// <summary>
///   Default <see cref="IScanTimerFactory"/> backed by <see cref="System.Threading.Timer"/>.
/// </summary>
public sealed class SystemScanTimerFactory : IScanTimerFactory
{
    /// <inheritdoc />
    public IScanTimer Create() => new SystemScanTimer();
}

internal sealed class SystemScanTimer : IScanTimer
{
    private readonly System.Threading.Timer timer;

    public SystemScanTimer() =>
        this.timer = new System.Threading.Timer(
            _ => Elapsed?.Invoke(this, EventArgs.Empty),
            null,
            Timeout.InfiniteTimeSpan,
            Timeout.InfiniteTimeSpan);

    public event EventHandler? Elapsed;

    public void Arm(TimeSpan delay) => this.timer.Change(delay, Timeout.InfiniteTimeSpan);

    public void Cancel() => this.timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);

    public void Dispose() => this.timer.Dispose();
}
