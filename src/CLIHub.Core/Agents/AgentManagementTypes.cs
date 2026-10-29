namespace CLIHub.Core.Agents;

/// <summary>
///   Result of an on-demand agent probe: host installation and the freshly probed version.
/// </summary>
/// <param name="IsInstalled"> Whether the agent is installed on the host. </param>
/// <param name="Version"> The freshly probed version, or null when unknown. </param>
public sealed record AgentProbeResult(bool IsInstalled, string? Version);

/// <summary>
///   Outcome of an agent update request.
/// </summary>
public enum AgentUpdateOutcome
{
    /// <summary>
    ///   The update command was launched.
    /// </summary>
    Started,

    /// <summary>
    ///   The agent does not declare an update command.
    /// </summary>
    Unsupported,

    /// <summary>
    ///   A matching agent process is running.
    /// </summary>
    Blocked,

    /// <summary>
    ///   The update command could not be started.
    /// </summary>
    Failed
}

/// <summary>
///   Result of an agent update request.
/// </summary>
/// <param name="Outcome"> The outcome. </param>
/// <param name="Message">
///   A user-facing message when the outcome is not <see cref="AgentUpdateOutcome.Started" />.
/// </param>
public sealed record AgentUpdateResult(AgentUpdateOutcome Outcome, string? Message)
{
    /// <summary>
    ///   Whether the update command was started.
    /// </summary>
    public bool Started => this.Outcome == AgentUpdateOutcome.Started;
}
