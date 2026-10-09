namespace CLIHub.Core.Models;

/// <summary>
///   Represents a running agent process matched to a registered plugin.
/// </summary>
/// <param name="Plugin"> The plugin that this process belongs to. </param>
/// <param name="Process"> The running process instance. </param>
public record RunningAgent(
    Plugin Plugin,
    AgentProcessInstance Process);
