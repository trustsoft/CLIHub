using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CLIHub.Core.Services;

/// <summary>
/// Service for discovering and managing AI agent CLI tool plugins
/// </summary>
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly List<Plugin> _plugins = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
    }

    public void LoadPlugins()
    {
        _plugins.Clear();

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var pluginsPath = Path.Combine(appDataPath, "CLIHub", "plugins");

        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
            return;
        }

        var pluginDirectories = Directory.GetDirectories(pluginsPath);
        _logger.LogDebug("Scanning {Count} plugin directories under {Path}", pluginDirectories.Length, pluginsPath);

        foreach (var pluginDir in pluginDirectories)
        {
            var pluginJsonPath = Path.Combine(pluginDir, "plugin.json");
            
            if (!File.Exists(pluginJsonPath))
                continue;

            try
            {
                var json = File.ReadAllText(pluginJsonPath);
                var plugin = JsonSerializer.Deserialize<Plugin>(json, JsonOptions);

                if (plugin != null && ValidatePlugin(plugin, pluginDir))
                {
                    // Check for duplicate ID
                    if (IsDuplicateId(plugin.Id, pluginDir))
                    {
                        _logger.LogWarning("Duplicate plugin ID {PluginId} in {Directory}; skipped", plugin.Id, pluginDir);
                        continue;
                    }

                    plugin.PluginDirectory = pluginDir;
                    
                    // Load logo with placeholder fallback
                    plugin.LogoPath = LoadPluginLogo(pluginDir);

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

    public IEnumerable<Plugin> GetAllPlugins()
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

        if (plugin.Commands == null || plugin.Commands.Count == 0)
        {
            errors.Add("Plugin must have at least one command");
        }

        if (errors.Count > 0)
        {
            _logger.LogWarning("Invalid plugin in {Directory}: {Errors}", pluginDirectory, string.Join("; ", errors));
            return false;
        }

        return true;
    }

    private string LoadPluginLogo(string pluginDirectory)
    {
        var logoPath = Path.Combine(pluginDirectory, "logo.png");
        
        if (File.Exists(logoPath))
        {
            return logoPath;
        }

        // Return path to default placeholder logo
        var defaultLogoPath = Path.Combine(pluginDirectory, "assets", "placeholder-logo.png");
        
        // Create assets directory if it doesn't exist
        var assetsDir = Path.Combine(pluginDirectory, "assets");
        if (!Directory.Exists(assetsDir))
        {
            Directory.CreateDirectory(assetsDir);
        }

        // Copy placeholder if it doesn't exist in plugin directory
        if (!File.Exists(defaultLogoPath))
        {
            CreatePlaceholderLogo(defaultLogoPath);
        }

        return defaultLogoPath;
    }

    private void CreatePlaceholderLogo(string logoPath)
    {
        // Create a simple 64x64 transparent PNG as placeholder
        // For MVP, we'll create a minimal valid PNG file
        var placeholderBytes = new byte[]
        {
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00, 0x40, // 64x64
            0x08, 0x06, 0x00, 0x00, 0x00, 0x99, 0x5F, 0x62, 0x6B,
            0x00, 0x00, 0x00, 0x01, 0x73, 0x52, 0x47, 0x42, // sRGB chunk
            0x00, 0xAE, 0xCE, 0x1C, 0xE9,
            0x00, 0x00, 0x00, 0x1F, 0x49, 0x44, 0x41, 0x54, // IDAT chunk
            0x78, 0x9C, 0x63, 0x60, 0x18, 0x05, 0xA3, 0x60, 0x14, 0x8C,
            0x82, 0x51, 0x30, 0x0A, 0x46, 0xC1, 0x28, 0x18, 0x05, 0xA3,
            0x60, 0x14, 0x8C, 0x82, 0x51, 0x30, 0x0A, 0x46, 0xC1, 0x28,
            0x18, 0x05, 0xA3, 0x60, 0x14, 0x8C, 0x82, 0x51, 0x30, 0x0A,
            0x46, 0x01, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, // IEND chunk
            0xAE, 0x42, 0x60, 0x82
        };

        File.WriteAllBytes(logoPath, placeholderBytes);
    }

    private bool IsDuplicateId(string pluginId, string currentPluginDirectory)
    {
        return _plugins.Any(p => p.Id == pluginId && p.PluginDirectory != currentPluginDirectory);
    }

    public Plugin? GetPluginById(string id)
    {
        return _plugins.FirstOrDefault(p => p.Id == id);
    }
}