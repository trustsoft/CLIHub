namespace CLIHub.Models;

/// <summary>
/// Represents an AI agent CLI tool plugin loaded from plugin.json
/// </summary>
public class Plugin
{
    /// <summary>
    /// Unique identifier for the plugin (matches subdirectory name)
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name of the plugin
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of what the plugin does
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// List of commands provided by this plugin
    /// </summary>
    public List<PluginCommand> Commands { get; set; } = new();

    /// <summary>
    /// Path to the plugin's logo image (typically logo.png in plugin directory)
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    /// Directory path where the plugin.json was loaded from
    /// </summary>
    public string? PluginDirectory { get; set; }
}
