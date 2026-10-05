namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Executes an agent command with a selected project context.
/// </summary>
public interface IAgentCommandWorkflow
{
    /// <summary>
    ///   Executes a command for the plugin in the project directory.
    /// </summary>
    /// <param name="plugin"> The agent plugin. </param>
    /// <param name="project"> The project context. </param>
    /// <param name="kind"> The command to execute. </param>
    /// <param name="cancellationToken"> Cancellation requested by the caller. </param>
    /// <returns> The command result. </returns>
    Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        Project project,
        AgentCommandKind kind,
        CancellationToken cancellationToken = default);
}
