namespace CLIHub.Core.Models;

/// <summary>
///   Represents a single command exposed by an agent.
/// </summary>
public class PluginCommand
{
    /// <summary>
    ///   Optional display label. Command identity comes from its position in
    ///   <see cref="AgentCommands"/>.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///   Executable to launch (for example "opencode", "npm").
    /// </summary>
    public required string Executable { get; set; }

    /// <summary>
    ///   Optional command-line arguments to pass to the executable.
    /// </summary>
    public string? Arguments { get; set; }
}
