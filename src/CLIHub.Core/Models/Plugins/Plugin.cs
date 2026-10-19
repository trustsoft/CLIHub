namespace CLIHub.Core.Models;

/// <summary>
///   Represents an AI agent CLI tool plugin loaded from plugin.json.
/// </summary>
public class Plugin
{
    /// <summary>
    ///   Unique identifier for the plugin (matches subdirectory name).
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    ///   Display name of the plugin.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    ///   Description of what the plugin does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///   The named command set exposed by this agent.
    /// </summary>
    public AgentCommands Commands { get; set; } = new();

    /// <summary>
    ///   Folder/file markers used to detect host and project availability.
    /// </summary>
    public AgentDetection Detection { get; set; } = new();

    /// <summary>
    ///   Path to the plugin's logo image (typically logo.png in plugin directory).
    /// </summary>
    public string? LogoPath { get; set; }

    /// <summary>
    ///   Directory path where the plugin.json was loaded from.
    /// </summary>
    public string? PluginDirectory { get; set; }

    /// <summary>
    ///   Creates a detached copy of this plugin. Catalog read methods hand out copies, so callers
    ///   can mutate a returned plugin — including its commands and detection markers — without
    ///   corrupting the loaded catalog.
    /// </summary>
    /// <returns> A new plugin with the same scalar values and cloned command and detection state. </returns>
    public Plugin Copy()
    {
        var copy = new Plugin
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            LogoPath = this.LogoPath,
            PluginDirectory = this.PluginDirectory,
            Commands = this.Commands,
            Detection = new AgentDetection
            {
                SystemPaths = this.Detection?.SystemPaths is { } systemPaths ? [.. systemPaths] : [],
                ProjectIndicators = this.Detection?.ProjectIndicators is { } projectIndicators ? [.. projectIndicators] : []
            }
        };

        if (this.Commands is { } commands)
        {
            // Resolve every kind through Get, the same lookup consumers use, so the copy honors
            // whatever AgentCommands defines instead of assuming a fixed set of slots.
            copy.Commands = new AgentCommands
            {
                Launch = CopyCommand(commands.Get(AgentCommandKind.Launch)),
                Resume = CopyCommand(commands.Get(AgentCommandKind.Resume)),
                Version = CopyCommand(commands.Get(AgentCommandKind.Version)),
                Update = CopyCommand(commands.Get(AgentCommandKind.Update))
            };
        }

        return copy;
    }

    private static PluginCommand? CopyCommand(PluginCommand? command) => command is null
        ? null
        : new PluginCommand
        {
            Name = command.Name,
            Executable = command.Executable,
            Arguments = command.Arguments
        };
}
