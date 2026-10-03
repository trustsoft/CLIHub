namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Provides access to the application configuration.
/// </summary>
public interface IConfigService
{
    /// <summary>
    ///   Loads the configuration from disk.
    /// </summary>
    /// <returns> The loaded configuration, or the default configuration when the file does not exist. </returns>
    AppConfig Load();

    /// <summary>
    ///   Saves the configuration to disk.
    /// </summary>
    /// <param name="config"> The configuration to save. </param>
    void Save(AppConfig config);

    /// <summary>
    ///   Gets the currently selected project.
    /// </summary>
    /// <returns> The current project, or null when none is selected. </returns>
    Project? GetCurrentProject();

    /// <summary>
    ///   Sets the currently selected project.
    /// </summary>
    /// <param name="projectId"> The ID of the project to select, or null to clear the selection. </param>
    void SetCurrentProject(string? projectId);

    /// <summary>
    ///   The path to the config.json file.
    /// </summary>
    string ConfigFilePath { get; }
}
