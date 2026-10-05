namespace CLIHub;

using CLIHub.Core.Plugins;

/// <summary>
///   Coordinates first-run plugin seeding and plugin catalog loading.
/// </summary>
public sealed class PluginInitializationService : IPluginInitializationService
{
    private readonly IPluginSeeder _pluginSeeder;
    private readonly IPluginManager _pluginManager;

    /// <summary>
    ///   Creates the startup service with the plugin seeder and manager.
    /// </summary>
    /// <param name="pluginSeeder"> Seeds built-in plugin descriptors and logos. </param>
    /// <param name="pluginManager"> Loads the plugin catalog. </param>
    public PluginInitializationService(IPluginSeeder pluginSeeder, IPluginManager pluginManager)
    {
        _pluginSeeder = pluginSeeder ?? throw new ArgumentNullException(nameof(pluginSeeder));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
    }

    /// <inheritdoc />
    public void Initialize()
    {
        _pluginSeeder.SeedIfEmpty();
        _pluginManager.LoadPlugins();
    }
}
