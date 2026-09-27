namespace CLIHub.Core.Models;

/// <summary>
/// The kinds of command an AI agent can expose.
/// </summary>
public enum AgentCommandKind
{
    /// <summary>Start a new agent session in the project.</summary>
    Launch,

    /// <summary>Resume the previous agent session.</summary>
    Resume,

    /// <summary>Report the agent's version.</summary>
    Version,

    /// <summary>Update the agent.</summary>
    Update,

    /// <summary>Initialize the agent in the project.</summary>
    Init
}
