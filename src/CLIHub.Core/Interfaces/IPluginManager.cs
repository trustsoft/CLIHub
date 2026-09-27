using CLIHub.Core.Models;

namespace CLIHub.Core.Interfaces;

/// <summary>
/// Service for discovering and managing AI agent CLI tool plugins
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// Load all plugins from the plugins directory
    /// </summary>
    void LoadPlugins();

    /// <summary>
    /// Get all loaded plugins
    /// </summary>
    /// <returns>Collection of loaded plugins</returns>
    IEnumerable<Plugin> GetAllPlugins();

    /// <summary>
    /// Get a specific plugin by ID
    /// </summary>
    /// <param name="id">Plugin ID</param>
    /// <returns>Plugin if found, null otherwise</returns>
    Plugin? GetPluginById(string id);
}
