namespace CLIHub.Core.Processes;

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
}
