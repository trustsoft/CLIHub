namespace CLIHub.Core.Plugins;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Models;

/// <summary>
///   Discovers and stores AI agent CLI tool plugins.
/// </summary>
public sealed class PluginCatalog : IPluginCatalog
{
    private readonly ILogger<PluginCatalog> logger;
    private readonly PluginLogoResolver logoResolver;
    private readonly IPluginDescriptorReader descriptorReader;
    private readonly IPluginDescriptorValidator descriptorValidator;
    private readonly string pluginsPath;
    private IReadOnlyList<Plugin> plugins = Array.Empty<Plugin>();

    /// <inheritdoc />
    public event EventHandler? PluginsChanged;

    /// <summary>
    ///   Creates the plugin catalog for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="logoResolver"> The resolver producing final plugin logo paths. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c> %APPDATA%\CLIHub\plugins </c>. </param>
    /// <param name="descriptorReader"> The reader for plugin.json descriptors. </param>
    /// <param name="descriptorValidator"> The validator for plugin descriptors. </param>
    public PluginCatalog(ILogger<PluginCatalog> logger,
                         PluginLogoResolver logoResolver,
                         string? pluginsPath = null,
                         IPluginDescriptorReader? descriptorReader = null,
                         IPluginDescriptorValidator? descriptorValidator = null)
    {
        this.logger = logger;
        this.logoResolver = logoResolver ?? throw new ArgumentNullException(nameof(logoResolver));
        this.pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
        this.descriptorReader = descriptorReader ?? new PluginDescriptorReader();
        this.descriptorValidator = descriptorValidator ?? new PluginDescriptorValidator();
    }

    /// <inheritdoc />
    public void LoadPlugins()
    {
        this.plugins = this.LoadPluginsCore();
    }

    /// <inheritdoc />
    public void ReloadPlugins()
    {
        this.plugins = this.LoadPluginsCore();
        this.PluginsChanged?.Invoke(this, EventArgs.Empty);
    }

    private IReadOnlyList<Plugin> LoadPluginsCore()
    {
        var plugins = new List<Plugin>();

        var pluginsPath = this.pluginsPath;

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
            return plugins;
        }

        var pluginDirectories = Directory.GetDirectories(pluginsPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
        this.logger.LogDebug("Scanning {Count} plugin directories under {Path}", pluginDirectories.Length, pluginsPath);

        foreach (var pluginDir in pluginDirectories)
        {
            try
            {
                var descriptor = this.descriptorReader.Read(pluginDir);
                if (descriptor.Error is not null)
                {
                    this.logger.LogWarning(descriptor.Error, "Failed to load plugin.json from {Directory}", pluginDir);
                    continue;
                }

                var plugin = descriptor.Plugin;

                if (plugin != null && this.ValidatePlugin(plugin, pluginDir))
                {
                    var loadedDirectory = FindLoadedPluginDirectory(plugins, plugin.Id, pluginDir);
                    if (loadedDirectory is not null)
                    {
                        this.logger.LogWarning(
                            "Duplicate plugin ID {PluginId} in {Directory}; skipped; loaded from {LoadedDirectory}",
                            plugin.Id,
                            pluginDir,
                            loadedDirectory);
                        continue;
                    }

                    plugin.PluginDirectory = pluginDir;
                    plugin.LogoPath = this.logoResolver.Resolve(plugin.Id, pluginDir);

                    plugins.Add(plugin);
                    this.logger.LogInformation("Loaded plugin {PluginId} ({PluginName})", plugin.Id, plugin.Name);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to load plugin.json from {Directory}", pluginDir);
            }
        }

        this.logger.LogInformation("Loaded {Count} plugin(s)", plugins.Count);
        return plugins;
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> GetAllPlugins()
    {
        return this.plugins;
    }

    private bool ValidatePlugin(Plugin plugin, string pluginDirectory)
    {
        var result = this.descriptorValidator.Validate(plugin);
        if (!result.IsValid)
        {
            this.logger.LogWarning(
                "Invalid plugin in {Directory}: {Errors}",
                pluginDirectory,
                string.Join("; ", result.Errors));
            return false;
        }

        return true;
    }

    private static string? FindLoadedPluginDirectory(IReadOnlyList<Plugin> plugins,
                                                     string pluginId,
                                                     string currentPluginDirectory)
    {
        return plugins
            .Where(plugin => plugin.Id == pluginId && plugin.PluginDirectory != currentPluginDirectory)
            .Select(plugin => plugin.PluginDirectory)
            .FirstOrDefault();
    }
}
