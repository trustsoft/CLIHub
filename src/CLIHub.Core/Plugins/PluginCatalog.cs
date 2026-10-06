namespace CLIHub.Core.Plugins;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;

/// <summary>
///   Discovers and stores AI agent CLI tool plugins.
/// </summary>
public sealed class PluginCatalog : IPluginCatalog
{
    private readonly ILogger<PluginCatalog> _logger;
    private readonly ILogoCacheService _logoCache;
    private readonly IPluginDescriptorReader _descriptorReader;
    private readonly IPluginDescriptorValidator _descriptorValidator;
    private readonly string _pluginsPath;
    private readonly List<Plugin> _plugins = new();

    /// <summary>
    ///   Creates the plugin catalog for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="logoCache"> The persistent logo cache resolving plugin logos. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c>%APPDATA%\CLIHub\plugins</c>. </param>
    /// <param name="descriptorReader"> The reader for plugin.json descriptors. </param>
    /// <param name="descriptorValidator"> The validator for plugin descriptors. </param>
    public PluginCatalog(
        ILogger<PluginCatalog> logger,
        ILogoCacheService logoCache,
        string? pluginsPath = null,
        IPluginDescriptorReader? descriptorReader = null,
        IPluginDescriptorValidator? descriptorValidator = null)
    {
        _logger = logger;
        _logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
        _pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
        _descriptorReader = descriptorReader ?? new PluginDescriptorReader();
        _descriptorValidator = descriptorValidator ?? new PluginDescriptorValidator();
    }

    /// <inheritdoc />
    public void LoadPlugins()
    {
        _plugins.Clear();

        var pluginsPath = _pluginsPath;

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
            return;
        }

        var pluginDirectories = Directory.GetDirectories(pluginsPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
        _logger.LogDebug("Scanning {Count} plugin directories under {Path}", pluginDirectories.Length, pluginsPath);

        foreach (var pluginDir in pluginDirectories)
        {
            try
            {
                var descriptor = _descriptorReader.Read(pluginDir);
                if (descriptor.Error is not null)
                {
                    _logger.LogWarning(descriptor.Error, "Failed to load plugin.json from {Directory}", pluginDir);
                    continue;
                }

                var plugin = descriptor.Plugin;

                if (plugin != null && ValidatePlugin(plugin, pluginDir))
                {
                    var loadedDirectory = FindLoadedPluginDirectory(plugin.Id, pluginDir);
                    if (loadedDirectory is not null)
                    {
                        _logger.LogWarning(
                            "Duplicate plugin ID {PluginId} in {Directory}; skipped; loaded from {LoadedDirectory}",
                            plugin.Id,
                            pluginDir,
                            loadedDirectory);
                        continue;
                    }

                    plugin.PluginDirectory = pluginDir;
                    plugin.LogoPath = _logoCache.GetOrResolve(
                        $"plugin:{plugin.Id}",
                        () => LoadPluginLogo(pluginDir));

                    _plugins.Add(plugin);
                    _logger.LogInformation("Loaded plugin {PluginId} ({PluginName})", plugin.Id, plugin.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load plugin.json from {Directory}", pluginDir);
            }
        }

        _logger.LogInformation("Loaded {Count} plugin(s)", _plugins.Count);
    }

    /// <inheritdoc />
    public IReadOnlyList<Plugin> GetAllPlugins()
    {
        return _plugins;
    }

    private bool ValidatePlugin(Plugin plugin, string pluginDirectory)
    {
        var result = _descriptorValidator.Validate(plugin);
        if (!result.IsValid)
        {
            _logger.LogWarning(
                "Invalid plugin in {Directory}: {Errors}",
                pluginDirectory,
                string.Join("; ", result.Errors));
            return false;
        }

        return true;
    }

    private static string? LoadPluginLogo(string pluginDirectory)
    {
        var logoPath = Path.Combine(pluginDirectory, "logo.png");

        return File.Exists(logoPath) ? logoPath : null;
    }

    private string? FindLoadedPluginDirectory(string pluginId, string currentPluginDirectory)
    {
        return _plugins
            .Where(plugin => plugin.Id == pluginId && plugin.PluginDirectory != currentPluginDirectory)
            .Select(plugin => plugin.PluginDirectory)
            .FirstOrDefault();
    }
}
