namespace CLIHub.Tests.Services;

/// <summary>
///   Controllable <see cref="TimeProvider"/> for testing time-based caching.
/// </summary>
public sealed class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset now;

    public FakeTimeProvider(DateTimeOffset start) => this.now = start;

    public override DateTimeOffset GetUtcNow() => this.now;

    public void Advance(TimeSpan by) => this.now += by;
}
