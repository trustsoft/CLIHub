namespace CLIHub;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

/// <summary>
///   Thin application workflow that supplies project context to agent command execution.
/// </summary>
public sealed class AgentCommandWorkflow : IAgentCommandWorkflow
{
    private readonly IAgentCommandService _agentCommands;

    /// <summary>
    ///   Creates the workflow over the Core agent command service.
    /// </summary>
    /// <param name="agentCommands"> Agent command service. </param>
    public AgentCommandWorkflow(IAgentCommandService agentCommands)
    {
        _agentCommands = agentCommands ?? throw new ArgumentNullException(nameof(agentCommands));
    }

    /// <inheritdoc />
    public Task<AgentCommandResult> ExecuteAsync(
        Plugin plugin,
        Project project,
        AgentCommandKind kind,
        CancellationToken cancellationToken = default) =>
        _agentCommands.ExecuteAsync(plugin, kind, project.Path, cancellationToken);
}
