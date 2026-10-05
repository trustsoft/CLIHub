namespace CLIHub.Core.Models;

/// <summary>
///   Persistence representation of the existing flat config.json document.
/// </summary>
internal sealed class AppConfigDocument
{
    /// <summary>
    ///   The schema version written by the current configuration serializer.
    /// </summary>
    public int? SchemaVersion { get; set; }

    /// <summary>
    ///   The current configuration schema version.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    ///   The schema version represented by documents without version metadata.
    /// </summary>
    public const int LegacySchemaVersion = 0;

    /// <summary>
    ///   Projects stored in the configuration document.
    /// </summary>
    public List<Project> Projects { get; set; } = new();

    /// <summary>
    ///   User preferences stored in the configuration document.
    /// </summary>
    public AppPreferences Preferences { get; set; } = new();

    /// <summary>
    ///   ID of the current project stored in the configuration document.
    /// </summary>
    public string? CurrentProjectId { get; set; }

    /// <summary>
    ///   Creates the persistence document from the public configuration model.
    /// </summary>
    /// <param name="config"> The configuration to represent. </param>
    /// <returns> The persistence representation. </returns>
    public static AppConfigDocument From(AppConfig config) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        Projects = config.Projects,
        Preferences = config.Preferences,
        CurrentProjectId = config.CurrentProjectId
    };

    /// <summary>
    ///   Creates the public configuration model from the persistence document.
    /// </summary>
    /// <returns> The public configuration model. </returns>
    public AppConfig ToAppConfig() => new()
    {
        Projects = Projects,
        Preferences = Preferences,
        CurrentProjectId = CurrentProjectId
    };
}
