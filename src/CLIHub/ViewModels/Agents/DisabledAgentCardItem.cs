namespace CLIHub.ViewModels;

using CLIHub.Core.Plugins;

/// <summary>
///   A card for a plugin disabled in the plugin state file. Disabled plugins are not loaded
///   and not launchable; the card carries the metadata the Disabled section renders and the
///   Enable action re-enables the plugin through the management view model.
/// </summary>
public sealed class DisabledAgentCardItem
{
    /// <summary>
    ///   Creates a card from a catalog disabled-plugin entry.
    /// </summary>
    /// <param name="entry"> The disabled catalog entry. </param>
    public DisabledAgentCardItem(DisabledPluginEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        this.PluginId = entry.PluginId;
        this.Name = entry.PluginId;
        this.PluginDirectory = entry.PluginDirectory;
        this.Origin = entry.Origin;
        this.Version = entry.Version;
        this.LogoPath = null;
    }

    /// <summary> The plugin id. </summary>
    public string PluginId { get; }

    /// <summary> The display name (equals plugin id for disabled plugins). </summary>
    public string Name { get; }

    /// <summary> The plugin's directory on disk. </summary>
    public string PluginDirectory { get; }

    /// <summary> The path to the plugin's logo, or null for disabled plugins (use a fallback). </summary>
    public string? LogoPath { get; }

    /// <summary>
    ///   The recorded origin from the plugin state file, or null when no state entry exists.
    /// </summary>
    public string? Origin { get; }

    /// <summary> The plugin's declared version, or null when unknown. </summary>
    public string? Version { get; }

    /// <summary>
    ///   The agent name's tooltip: the recorded origin as a labelled line ("Origin: Built-in",
    ///   "Origin: Installed", or "Origin: Unknown" — plugins without a state entry are
    ///   unknown-origin), followed by the plugin's declared version when known. No publisher
    ///   or homepage lines: the descriptor is not loaded for disabled plugins.
    /// </summary>
    public string IdentityTooltip
    {
        get
        {
            var lines = new List<string>
            {
                $"Origin: {this.Origin switch
                {
                    PluginOrigin.Builtin => "Built-in",
                    PluginOrigin.Installed => "Installed",
                    _ => "Unknown"
                }}"
            };

            if (this.Version is { } version)
            {
                lines.Add($"Version: {version}");
            }

            return string.Join("\n", lines);
        }
    }

    /// <summary> The version chip text, or "Unknown version" when the version is not known. </summary>
    public string VersionChipText => this.Version is { } version ? $"{version}" : "Unknown version";
}
