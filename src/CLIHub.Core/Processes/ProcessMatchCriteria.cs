namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
/// Criteria for matching agent processes.
/// </summary>
public record ProcessMatchCriteria
{
    /// <summary>
    /// Gets the plugin to match against.
    /// </summary>
    public required Plugin Plugin { get; init; }

    /// <summary>
    /// Gets the matching fidelity level.
    /// </summary>
    public required AgentProcessMatchDetail Detail { get; init; }

    /// <summary>
    /// Gets the timeout for the matching operation, if specified.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Creates a new <see cref="ProcessMatchCriteria"/> instance.
    /// </summary>
    /// <param name="plugin">The plugin to match against.</param>
    /// <param name="detail">The matching fidelity level.</param>
    /// <param name="timeout">The timeout for the matching operation.</param>
    /// <returns>A new <see cref="ProcessMatchCriteria"/> instance.</returns>
    public static ProcessMatchCriteria Create(
        Plugin plugin,
        AgentProcessMatchDetail detail = AgentProcessMatchDetail.CommandLine,
        TimeSpan? timeout = null)
        => new() { Plugin = plugin, Detail = detail, Timeout = timeout };
}
