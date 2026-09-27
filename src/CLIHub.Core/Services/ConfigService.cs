using CLIHub.Core.Models;
using CLIHub.Core.Interfaces;
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

    private AppConfig? _cachedConfig;

    public string ConfigFilePath { get; }

    public ConfigService()
    {
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
            _cachedConfig = new AppConfig();
            Save(_cachedConfig);
            return _cachedConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            _cachedConfig = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            return _cachedConfig;
        }
        catch
        {
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
