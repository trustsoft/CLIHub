namespace CLIHub.Core.Plugins;

/// <summary>
///   Maintains the built-in plugin descriptors in the plugins folder: seeds them on first run
///   and refreshes them on application updates, using three-hash comparison so unmodified
///   built-ins are corrected while user modifications are preserved.
/// </summary>
public interface IPluginSeeder
{
    /// <summary>
    ///   Ensures every built-in descriptor and logo exists and is current: writes missing
    ///   built-ins, overwrites unmodified built-ins whose embedded content changed (application
    ///   update), and never touches installed, disabled, or user-modified plugins.
    /// </summary>
    /// <returns> The number of descriptors written or refreshed. </returns>
    int EnsureBuiltinsExist();
}
