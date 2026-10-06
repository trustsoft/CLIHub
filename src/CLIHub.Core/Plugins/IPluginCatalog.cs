namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Loads and exposes the current set of AI agent CLI tool plugins.
/// </summary>
public interface IPluginCatalog
{
    /// <summary>
    ///   Raised after a manual reload replaces the current plugin snapshot.
    /// </summary>
    event EventHandler? PluginsChanged;

    /// <summary>
    ///   Loads all plugins from the plugins directory.
    /// </summary>
    void LoadPlugins();

    /// <summary>
    ///   Reloads all plugins from the plugins directory and notifies subscribers.
    /// </summary>
    void ReloadPlugins();

    /// <summary>
    ///   Gets all loaded plugins.
    /// </summary>
    /// <returns> The read-only collection of loaded plugins. </returns>
    IReadOnlyList<Plugin> GetAllPlugins();
}
