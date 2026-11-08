namespace CLIHub.Core.Plugins;

/// <summary>
///   A plugin skipped during catalog load because it is marked disabled in the plugin state
///   file. Disabled plugins stay on disk and can be re-enabled; the UI renders them in a
///   separate section with an Enable action.
/// </summary>
/// <param name="PluginId"> The plugin id. </param>
/// <param name="PluginDirectory"> The plugin's directory on disk. </param>
/// <param name="Origin">
///   The recorded origin (<see cref="PluginOrigin" /> values), or null when no state entry
///   exists.
/// </param>
/// <param name="Version"> The plugin's declared version, or null when unknown. </param>
public sealed record DisabledPluginEntry(
    string PluginId,
    string PluginDirectory,
    string? Origin,
    string? Version);
