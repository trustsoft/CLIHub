namespace CLIHub.Core.Models;

public sealed record UpdateCheckResult(bool Available, string? Version)
{
    public static UpdateCheckResult None { get; } = new(false, null);

    public static UpdateCheckResult Found(string version) => new(true, version);
}
