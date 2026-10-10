namespace CLIHub.Tests.Scanning;

using CLIHub.Core.Scanning;

internal sealed class FakeRepositoryWatcher : IRepositoryWatcher
{
    public event EventHandler? Changed;
    public event EventHandler? Overflow;

    public bool Started { get; private set; }
    public bool Disposed { get; private set; }

    public void Start() => Started = true;

    public void Stop() => Started = false;

    public void Dispose() => Disposed = true;

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void RaiseOverflow() => Overflow?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeRepositoryWatcherFactory : IRepositoryWatcherFactory
{
    public List<FakeRepositoryWatcher> Watchers { get; } = new();
    public List<IReadOnlyList<string>> CreatedRoots { get; } = new();

    public IRepositoryWatcher Create(IReadOnlyList<string> roots)
    {
        CreatedRoots.Add(roots);
        var watcher = new FakeRepositoryWatcher();
        Watchers.Add(watcher);
        return watcher;
    }
}

internal sealed class FakeScanTimer : IScanTimer
{
    public event EventHandler? Elapsed;

    public TimeSpan? Armed { get; private set; }
    public bool Disposed { get; private set; }

    public void Arm(TimeSpan delay) => Armed = delay;

    public void Cancel() => Armed = null;

    public void Dispose() => Disposed = true;

    public void Fire() => Elapsed?.Invoke(this, EventArgs.Empty);
}

internal sealed class FakeScanTimerFactory : IScanTimerFactory
{
    public List<FakeScanTimer> Timers { get; } = new();

    public IScanTimer Create()
    {
        var timer = new FakeScanTimer();
        Timers.Add(timer);
        return timer;
    }
}

internal sealed class InlineSynchronizationContext : SynchronizationContext
{
    public override void Post(SendOrPostCallback d, object? state) => d(state);
}
