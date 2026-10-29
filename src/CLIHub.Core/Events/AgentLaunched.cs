namespace CLIHub.Core.Events;

using CLIHub.Core.Models;

/// <summary>
///   Published when a session-launching interactive agent command (Launch or Resume) succeeds.
///   ProjectPath is the launch working directory, or null when there is no project context.
/// </summary>
/// <param name="PluginId"> The unique identifier of the agent plugin. </param>
/// <param name="ProjectPath"> The file system path the agent was launched in, or null when there is no project context. </param>
/// <param name="CommandKind"> The command kind (Launch or Resume). </param>
/// <param name="LaunchedAt"> The UTC timestamp when the agent was launched. </param>
public record AgentLaunched(
    string PluginId,
    string? ProjectPath,
    AgentCommandKind CommandKind,
    DateTimeOffset LaunchedAt) : IDomainEvent;
