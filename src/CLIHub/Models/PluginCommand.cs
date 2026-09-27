namespace CLIHub.Models;

/// <summary>
/// Represents a command provided by a plugin
/// </summary>
public class PluginCommand
{
    /// <summary>
    /// Display name of the command
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Executable to launch (e.g., "aider", "claude", "copilot")
    /// </summary>
    public required string Executable { get; set; }

    /// <summary>
    /// Optional command-line arguments to pass to the executable
    /// </summary>
    public string? Arguments { get; set; }
}
