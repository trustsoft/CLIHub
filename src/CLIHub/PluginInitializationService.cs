namespace CLIHub;

using CLIHub.Core.Plugins;

/// <summary>
///   Coordinates first-run plugin seeding and plugin catalog loading.
/// </summary>
public sealed class PluginInitializationService : IPluginInitializationService
{
    private readonly IPluginSeeder _pluginSeeder;
    private readonly IPluginCatalog _pluginCatalog;

    /// <summary>
    ///   Creates the startup service with the plugin seeder and manager.
    /// </summary>
    /// <param name="pluginSeeder"> Seeds built-in plugin descriptors and logos. </param>
    /// <param name="pluginCatalog"> Loads the plugin catalog. </param>
    public PluginInitializationService(IPluginSeeder pluginSeeder, IPluginCatalog pluginCatalog)
    {
        _pluginSeeder = pluginSeeder ?? throw new ArgumentNullException(nameof(pluginSeeder));
        _pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _pluginSeeder.SeedIfEmpty();
        _pluginCatalog.LoadPlugins();
    }
}
