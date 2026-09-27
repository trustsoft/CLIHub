using CLIHub.Core.Models;
using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CLIHub.Core.Services;

/// <summary>
/// Service for loading and saving application configuration to config.json
/// </summary>
public class ConfigService : IConfigService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<ConfigService> _logger;
    private AppConfig? _cachedConfig;

    public string ConfigFilePath { get; }

    public ConfigService(ILogger<ConfigService> logger)
    {
        _logger = logger;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var clihubFolder = Path.Combine(appDataPath, "CLIHub");
        Directory.CreateDirectory(clihubFolder);
        ConfigFilePath = Path.Combine(clihubFolder, "config.json");
    }

    public AppConfig Load()
    {
        if (_cachedConfig != null)
            return _cachedConfig;

        if (!File.Exists(ConfigFilePath))
        {
            _logger.LogInformation("No config found at {Path}; creating defaults", ConfigFilePath);
            _cachedConfig = new AppConfig();
            Save(_cachedConfig);
            return _cachedConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            _logger.LogDebug("Loaded configuration from {Path}", ConfigFilePath);
            return _cachedConfig;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read config at {Path}; using defaults", ConfigFilePath);
            _cachedConfig = new AppConfig();
            return _cachedConfig;
        }
    }

    public void Save(AppConfig config)
    {
        _cachedConfig = config;
        var json = JsonSerializer.Serialize(config, JsonOptions);
        
        // Atomic write: write to temp file, then rename to avoid corruption
        var tempPath = ConfigFilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, ConfigFilePath, overwrite: true);
        _logger.LogDebug("Saved configuration to {Path}", ConfigFilePath);
    }

    public Project? GetCurrentProject()
    {
        var config = Load();
        if (config.CurrentProjectId == null)
            return null;

        return config.Projects.FirstOrDefault(p => p.Id == config.CurrentProjectId);
    }

    public void SetCurrentProject(string? projectId)
    {
        var config = Load();
        config.CurrentProjectId = projectId;
        Save(config);
    }
}
