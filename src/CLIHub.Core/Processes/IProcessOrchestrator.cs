namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// Orchestrates all process operations: launching, capturing output, and finding running agents.
/// </summary>
public interface IProcessOrchestrator
{
    /// <summary>
    /// Launches an agent command in an interactive terminal.
    /// </summary>
    /// <param name="request">The launch request.</param>
    /// <returns>The result of the launch operation.</returns>
    Task<ProcessResult> LaunchInteractiveAsync(LaunchRequest request);

    /// <summary>
    /// Executes an agent command and captures its output.
    /// </summary>
    /// <param name="request">The launch request.</param>
    /// <returns>The result of the execution with captured output.</returns>
    Task<ProcessResult> CaptureOutputAsync(LaunchRequest request);

    /// <summary>
    /// Finds all running processes matching the specified plugin.
    /// </summary>
    /// <param name="criteria">The matching criteria.</param>
    /// <returns>A read-only list of matching agent processes.</returns>
    Task<IReadOnlyList<AgentProcess>> FindRunningAgentsAsync(
        ProcessMatchCriteria criteria);

    /// <summary>
    /// Resolves the available runtime executable for the specified kind.
    /// </summary>
    /// <param name="kind">The runtime kind to resolve.</param>
    /// <param name="cancellationToken">A cancellation token for the operation.</param>
    /// <returns>
    /// The resolved runtime executable, or null if the runtime is not available.
    /// </returns>
    Task<RuntimeExecutable?> ResolveRuntimeAsync(
        RuntimeKind kind,
        CancellationToken cancellationToken = default);
}
