namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Reads the latest published version of an agent from its declared distribution source.
/// </summary>
public interface IAgentReleaseClient
{
    /// <summary>
    ///   Returns the latest published version string from the declared source, or null when the
    ///   block is unsupported or malformed, the request fails or times out, or the response
    ///   cannot be parsed. Caller cancellation still propagates.
    /// </summary>
    /// <param name="updateCheck"> The agent's declared update source. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> The published version string, or null. </returns>
    Task<string?> GetLatestVersionAsync(PluginUpdateCheck updateCheck, CancellationToken cancellationToken = default);
}
