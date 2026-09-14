using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Tests;

internal sealed class StubClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } =
        new(2026, 9, 14, 10, 0, 0, TimeSpan.Zero);
}
