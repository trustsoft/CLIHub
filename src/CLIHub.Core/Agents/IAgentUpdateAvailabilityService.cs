namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Determines whether a newer version of an agent exists by comparing the probed installed
///   version with the version published by the agent's declared distribution source.
///   Results are advisory only and cached per agent.
/// </summary>
public interface IAgentUpdateAvailabilityService
{
    /// <summary>
    ///   Returns the agent's update availability, reusing the cached result until the configured
    ///   agent probe time-to-live elapses. Failures degrade to <see cref="AgentUpdateStatus.Unknown" />
    ///   instead of throwing; caller cancellation still propagates.
    /// </summary>
    /// <param name="plugin"> The agent to check. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> The availability result. </returns>
    Task<AgentUpdateAvailability> CheckAsync(Plugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Clears all cached availability results so the next check runs again.
    /// </summary>
    void Invalidate();

    /// <summary>
    ///   Clears one agent's cached availability so its next check runs again. Other agents'
    ///   cached results and already-running checks are unaffected.
    /// </summary>
    /// <param name="plugin"> The agent whose cached availability is cleared. </param>
    void InvalidateFor(Plugin plugin);
}
