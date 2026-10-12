namespace CLIHub;

using CLIHub.Core.Plugins;

/// <summary>
///   Coordinates first-run plugin seeding and plugin catalog loading.
/// </summary>
public sealed class PluginInitializationService : IPluginInitializationService
{
    private readonly IPluginSeeder pluginSeeder;
    private readonly IPluginCatalog pluginCatalog;

    /// <summary>
    ///   Creates the startup service with the plugin seeder and manager.
    /// </summary>
    /// <param name="pluginSeeder"> Seeds built-in plugin descriptors and logos. </param>
    /// <param name="pluginCatalog"> Loads the plugin catalog. </param>
    public PluginInitializationService(IPluginSeeder pluginSeeder, IPluginCatalog pluginCatalog)
    {
        this.pluginSeeder = pluginSeeder ?? throw new ArgumentNullException(nameof(pluginSeeder));
        this.pluginCatalog = pluginCatalog ?? throw new ArgumentNullException(nameof(pluginCatalog));
    }

    /// <inheritdoc />
    public void Initialize()
    {
        this.pluginSeeder.SeedIfEmpty();
        this.pluginCatalog.LoadPlugins();
    }
}
