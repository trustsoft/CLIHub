namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   On-demand agent probe capability.
/// </summary>
public interface IAgentProbeService
{
    /// <summary>
    ///   Re-evaluates an agent's host installation and version, bypassing caches.
    /// </summary>
    /// <param name="plugin"> The agent to probe. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> The freshly probed installation state and version. </returns>
    Task<AgentProbeResult> ProbeAsync(Plugin plugin, CancellationToken cancellationToken = default);
}
