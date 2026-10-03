namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Executes a named agent command in the context of a project directory.
/// </summary>
public interface IAgentCommandService
{
    /// <summary>
    ///   Executes the given command kind for the plugin in the project folder.
    ///   Interactive commands open in a terminal; the version command captures output.
    /// </summary>
    Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        AgentCommandKind kind,
        string projectPath,
        CancellationToken cancellationToken = default);
}
