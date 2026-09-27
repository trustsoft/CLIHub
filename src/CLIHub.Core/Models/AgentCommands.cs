namespace CLIHub.Core.Models;

/// <summary>
/// The named command set an agent plugin exposes. Each command is optional except
/// <see cref="Launch"/>, which the loader requires.
/// </summary>
public class AgentCommands
{
    public PluginCommand? Launch { get; set; }
    public PluginCommand? Resume { get; set; }
    public PluginCommand? Version { get; set; }
    public PluginCommand? Update { get; set; }
    public PluginCommand? Init { get; set; }

    /// <summary>
    /// Returns the command for the given kind, or null when it is not defined.
    /// </summary>
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
