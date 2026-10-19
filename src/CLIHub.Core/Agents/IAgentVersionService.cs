namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Retrieves and caches the version of an agent, independent of any project.
/// </summary>
public interface IAgentVersionService
{
    /// <summary>
    ///   Returns the agent's version, or null when it has no version command or the
    ///   command fails. Results are cached per agent until <see cref="Invalidate" />.
    /// </summary>
    Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Clears the cached versions so the next lookup runs the command again.
    /// </summary>
    void Invalidate();

    /// <summary>
    ///   Clears one agent's cached version so its next lookup runs the command again.
    ///   Other agents' cached versions and already-running probes are unaffected.
    /// </summary>
    /// <param name="plugin"> The agent whose cached version is cleared. </param>
    void InvalidateFor(Plugin plugin);
}
