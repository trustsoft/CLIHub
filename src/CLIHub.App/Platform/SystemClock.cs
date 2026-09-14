using CLIHub.Core.Abstractions;

namespace CLIHub.App.Platform;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
