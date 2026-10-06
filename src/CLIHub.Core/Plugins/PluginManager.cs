namespace CLIHub.Core.Plugins;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.FileSystem;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;

/// <summary>
///   Discovers and manages AI agent CLI tool plugins.
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly ILogoCacheService _logoCache;
    private readonly IPluginDescriptorReader _descriptorReader;
    private readonly string _pluginsPath;
    private readonly List<Plugin> _plugins = new();

    /// <summary>
    ///   Creates the plugin manager for the given plugins root.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="logoCache"> The persistent logo cache resolving plugin logos. </param>
    /// <param name="pluginsPath"> Overrides the plugins root; defaults to <c>%APPDATA%\CLIHub\plugins</c>. </param>
    /// <param name="descriptorReader"> The reader for plugin.json descriptors. </param>
    public PluginManager(
        ILogger<PluginManager> logger,
        ILogoCacheService logoCache,
        string? pluginsPath = null,
        IPluginDescriptorReader? descriptorReader = null)
    {
        _logger = logger;
        _logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
        _pluginsPath = pluginsPath ?? AppPaths.PluginsDirectory;
        _descriptorReader = descriptorReader ?? new PluginDescriptorReader();
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

        var pluginDirectories = Directory.GetDirectories(pluginsPath);
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
                    // Check for duplicate ID
                    if (IsDuplicateId(plugin.Id, pluginDir))
                    {
                        _logger.LogWarning("Duplicate plugin ID {PluginId} in {Directory}; skipped", plugin.Id, pluginDir);
                        continue;
                    }

                    plugin.PluginDirectory = pluginDir;

                    // Resolve the plugin logo through the logo cache (logo.png or null).
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
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(plugin.Id))
        {
            errors.Add("Plugin ID is required");
        }

        if (string.IsNullOrWhiteSpace(plugin.Name))
        {
            errors.Add("Plugin Name is required");
        }

        if (plugin.Commands == null || plugin.Commands.Launch == null)
        {
            errors.Add("Plugin must define a launch command");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Invalid plugin in {Directory}: {Errors}", pluginDirectory, string.Join("; ", errors));
            return false;
        }

        return true;
    }

    /// <summary>
    ///   Scans the plugin folder for <c>logo.png</c> and returns its path, or null when the
    ///   folder has no logo.
    /// </summary>
    private static string? LoadPluginLogo(string pluginDirectory)
    {
        var logoPath = Path.Combine(pluginDirectory, "logo.png");

        return File.Exists(logoPath) ? logoPath : null;
    }

    private bool IsDuplicateId(string pluginId, string currentPluginDirectory)
    {
        return _plugins.Any(p => p.Id == pluginId && p.PluginDirectory != currentPluginDirectory);
    }
}
