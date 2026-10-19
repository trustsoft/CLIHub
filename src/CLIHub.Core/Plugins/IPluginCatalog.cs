namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Loads and exposes the current set of AI agent CLI tool plugins.
/// </summary>
/// <remarks>
///   <para>
///     Read methods return detached copies: mutating a returned plugin — including its commands
///     or detection state — does not affect the loaded catalog or any later read.
///   </para>
///   <para>
///     Plugin ids are loader-level identity and are compared with ordinal equality when duplicate
///     descriptors are resolved at load time: the first valid plugin in deterministic directory
///     order wins, and ids that differ only by case are distinct plugins.
///   </para>
/// </remarks>
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
    ///   Gets all loaded plugins as detached copies.
    /// </summary>
    /// <returns> The loaded plugins; safe to mutate or hold across asynchronous work. </returns>
    IReadOnlyList<Plugin> GetAllPlugins();
}
