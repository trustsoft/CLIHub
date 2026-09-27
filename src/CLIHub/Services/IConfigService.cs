using CLIHub.Models;

namespace CLIHub.Services;

/// <summary>
/// Service for loading and saving application configuration
/// </summary>
public interface IConfigService
{
    /// <summary>
    /// Load configuration from disk
    /// </summary>
    /// <returns>Loaded configuration, or default config if file doesn't exist</returns>
    AppConfig Load();

    /// <summary>
    /// Save configuration to disk
    /// </summary>
    /// <param name="config">Configuration to save</param>
    void Save(AppConfig config);

    /// <summary>
    /// Get the currently selected project
    /// </summary>
    /// <returns>Current project, or null if none selected</returns>
    Project? GetCurrentProject();

    /// <summary>
    /// Set the currently selected project
    /// </summary>
    /// <param name="projectId">ID of project to select, or null to clear selection</param>
    void SetCurrentProject(string? projectId);

    /// <summary>
    /// Path to the config.json file
    /// </summary>
    string ConfigFilePath { get; }
}
