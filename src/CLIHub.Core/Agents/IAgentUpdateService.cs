namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   On-demand agent update capability.
/// </summary>
public interface IAgentUpdateService
{
    /// <summary>
    ///   Runs an agent's update command interactively in the user profile directory,
    ///   unless the agent does not support updates or a matching agent process is running.
    /// </summary>
    /// <param name="plugin"> The agent to update. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> The update outcome. </returns>
    Task<AgentUpdateResult> UpdateAsync(Plugin plugin, CancellationToken cancellationToken = default);
}
