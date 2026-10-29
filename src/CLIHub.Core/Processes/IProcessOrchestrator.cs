namespace CLIHub.Core.Processes;

using CLIHub.Core.Infrastructure.Processes;
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
    /// Finds all running processes matching any of the specified plugins, with a single
    /// process enumeration.
    /// </summary>
    /// <param name="plugins">The plugins to match against.</param>
    /// <param name="detail">The matching fidelity level.</param>
    /// <returns>
    /// A read-only list of matched processes paired with the plugin they matched, at most one
    /// entry per process.
    /// </returns>
    Task<IReadOnlyList<PluginAgentProcess>> FindRunningAgentsAsync(
        IReadOnlyList<Plugin> plugins,
        AgentProcessMatchDetail detail);

}
