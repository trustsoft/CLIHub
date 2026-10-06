namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Loads and exposes the current set of AI agent CLI tool plugins.
/// </summary>
public interface IPluginCatalog
{
    /// <summary>
    ///   Loads all plugins from the plugins directory.
    /// </summary>
    void LoadPlugins();

    /// <summary>
    ///   Gets all loaded plugins.
    /// </summary>
    /// <returns> The read-only collection of loaded plugins. </returns>
    IReadOnlyList<Plugin> GetAllPlugins();
}
