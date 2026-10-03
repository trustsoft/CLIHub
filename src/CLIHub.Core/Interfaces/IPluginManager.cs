namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Discovers and manages AI agent CLI tool plugins.
/// </summary>
public interface IPluginManager
{
    /// <summary>
    ///   Loads all plugins from the plugins directory.
    /// </summary>
    void LoadPlugins();

    /// <summary>
    ///   Gets all loaded plugins.
    /// </summary>
    /// <returns> The collection of loaded plugins. </returns>
    IEnumerable<Plugin> GetAllPlugins();

    /// <summary>
    ///   Gets a specific plugin by ID.
    /// </summary>
    /// <param name="id"> The plugin ID. </param>
    /// <returns> The plugin when found; otherwise null. </returns>
    Plugin? GetPluginById(string id);
}
