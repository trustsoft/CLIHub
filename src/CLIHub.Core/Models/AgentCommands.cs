namespace CLIHub.Core.Models;

/// <summary>
///   The named command set an agent plugin exposes. Each command is optional except
///   <see cref="Launch"/>, which the loader requires.
/// </summary>
public class AgentCommands
{
    /// <summary>
    ///   Command that starts a new agent session in the project.
    /// </summary>
    public PluginCommand? Launch { get; set; }

    /// <summary>
    ///   Command that resumes the previous agent session.
    /// </summary>
    public PluginCommand? Resume { get; set; }

    /// <summary>
    ///   Command that reports the agent's version.
    /// </summary>
    public PluginCommand? Version { get; set; }

    /// <summary>
    ///   Command that updates the agent.
    /// </summary>
    public PluginCommand? Update { get; set; }

    /// <summary>
    ///   Command that initializes the agent in the project.
    /// </summary>
    public PluginCommand? Init { get; set; }

    /// <summary>
    ///   Returns the command for the given kind.
    /// </summary>
    /// <param name="kind"> The command kind to look up. </param>
    /// <returns> The command for the kind, or null when it is not defined. </returns>
    public PluginCommand? Get(AgentCommandKind kind) => kind switch
    {
        AgentCommandKind.Launch => Launch,
        AgentCommandKind.Resume => Resume,
        AgentCommandKind.Version => Version,
        AgentCommandKind.Update => Update,
        AgentCommandKind.Init => Init,
        _ => null
    };
}
