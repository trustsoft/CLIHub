namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
/// Finds running agent processes matching criteria.
/// </summary>
public interface IAgentProcessFinder
{
    /// <summary>
    /// Finds all running processes matching the specified criteria.
    /// </summary>
    /// <param name="criteria">The matching criteria.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>A read-only list of matching agent processes.</returns>
    Task<IReadOnlyList<AgentProcess>> FindAsync(
        ProcessMatchCriteria criteria,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds all running processes matching any of the specified plugins with a single
    /// process enumeration.
    /// </summary>
    /// <param name="plugins">The plugins to match against.</param>
    /// <param name="detail">The matching fidelity level.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of matched processes paired with the plugin they matched, at most one
    /// entry per process.
    /// </returns>
    Task<IReadOnlyList<PluginAgentProcess>> FindAllAsync(
        IReadOnlyList<Plugin> plugins,
        AgentProcessMatchDetail detail,
        CancellationToken cancellationToken = default);
}
