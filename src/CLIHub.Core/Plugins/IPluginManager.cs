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
    /// <returns> The read-only collection of loaded plugins. </returns>
    IReadOnlyList<Plugin> GetAllPlugins();
}
