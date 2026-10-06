namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Compatibility adapter for the plugin catalog contract.
/// </summary>
public sealed class PluginManager : IPluginManager
{
    private readonly IPluginCatalog _catalog;

    /// <summary>
    ///   Creates the compatibility adapter for the plugin catalog.
    /// </summary>
    /// <param name="catalog"> The shared plugin catalog. </param>
    public PluginManager(IPluginCatalog catalog)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
    }

    /// <inheritdoc />
    public void LoadPlugins()
    {
        _catalog.LoadPlugins();
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> GetAllPlugins()
    {
        return _catalog.GetAllPlugins();
    }
}
