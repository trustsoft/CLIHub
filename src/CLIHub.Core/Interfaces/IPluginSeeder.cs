namespace CLIHub.Core.Interfaces;

/// <summary>
///   Populates the plugins folder with built-in descriptors on first run.
/// </summary>
public interface IPluginSeeder
{
    /// <summary>
    ///   Seeds the plugins folder with the built-in descriptor set when it contains no
    ///   plugins. Never overwrites existing files.
    /// </summary>
    /// <returns> The number of descriptors written. </returns>
    int SeedIfEmpty();
}
