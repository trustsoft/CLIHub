namespace CLIHub.Core.Models;

public sealed record AgentHostStatus(bool HostInstalled, string? Version)
{
    public static AgentHostStatus NotInstalled { get; } = new(false, null);
}
