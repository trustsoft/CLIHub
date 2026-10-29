namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// A running agent process paired with the plugin it matched.
/// </summary>
/// <param name="Plugin"> The plugin whose match criteria the process satisfied. </param>
/// <param name="Process"> The matched running process. </param>
public sealed record PluginAgentProcess(
    Plugin Plugin,
    AgentProcess Process
);
