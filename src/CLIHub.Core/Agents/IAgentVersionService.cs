namespace CLIHub.Core.Agents;

using CLIHub.Core.Models;

/// <summary>
///   Retrieves and caches the version of an agent, independent of any project.
/// </summary>
public interface IAgentVersionService
{
    /// <summary>
    ///   Returns the agent's version, or null when it has no version command or the
    ///   command fails. Results are cached per agent until <see cref="Invalidate"/>.
    /// </summary>
    Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default);

    /// <summary>
    ///   Clears the cached versions so the next lookup runs the command again.
    /// </summary>
    void Invalidate();
}
