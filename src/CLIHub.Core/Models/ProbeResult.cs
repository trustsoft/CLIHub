namespace CLIHub.Core.Models;

public sealed record ProbeResult(int ExitCode, string? Output, bool TimedOut)
{
    public static ProbeResult Failed { get; } = new(-1, null, false);
}
