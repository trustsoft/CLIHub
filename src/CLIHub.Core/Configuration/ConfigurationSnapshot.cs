namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Detached, mutable-in-isolation configuration state used at the configuration boundary.
/// </summary>
public sealed class ConfigurationSnapshot
{
    /// <summary>
    ///   Registered projects in this snapshot.
    /// </summary>
    public List<Project> Projects { get; set; } = new();

    /// <summary>
    ///   User preferences in this snapshot.
    /// </summary>
    public AppPreferences Preferences { get; set; } = new();

    /// <summary>
    ///   ID of the currently selected project, or null when no project is selected.
    /// </summary>
    public string? CurrentProjectId { get; set; }

    /// <summary>
    ///   Creates a detached snapshot from the mutable application configuration.
    /// </summary>
    /// <param name="config"> The configuration to copy. </param>
    /// <returns> A detached snapshot. </returns>
    public static ConfigurationSnapshot From(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return new ConfigurationSnapshot
        {
            Projects = config.Projects.Select(CloneProject).ToList(),
            Preferences = ClonePreferences(config.Preferences),
            CurrentProjectId = config.CurrentProjectId
        };
    }

    /// <summary>
    ///   Creates a detached mutable application configuration for persistence.
    /// </summary>
    /// <returns> A detached application configuration. </returns>
    public AppConfig ToAppConfig() => new()
    {
        Projects = Projects.Select(CloneProject).ToList(),
        Preferences = ClonePreferences(Preferences),
        CurrentProjectId = CurrentProjectId
    };

    private static Project CloneProject(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Path = project.Path,
        IsFavorite = project.IsFavorite,
        LastUsed = project.LastUsed,
        LogoPath = project.LogoPath
    };

    private static AppPreferences ClonePreferences(AppPreferences preferences) => new()
    {
        StartWithWindows = preferences.StartWithWindows,
        Hotkey = preferences.Hotkey,
        TerminalExecutable = preferences.TerminalExecutable,
        DefaultRuntime = preferences.DefaultRuntime,
        LogLevel = preferences.LogLevel,
        ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents,
        AgentProbeTtlMinutes = preferences.AgentProbeTtlMinutes,
        AgentProbeTimeoutSeconds = preferences.AgentProbeTimeoutSeconds,
        CheckForUpdatesOnStartup = preferences.CheckForUpdatesOnStartup,
        ShowWindowOnStartup = preferences.ShowWindowOnStartup,
        PinLaunchWindow = preferences.PinLaunchWindow,
        PathDisplayStyle = preferences.PathDisplayStyle,
        LastSeenReleaseNotesVersion = preferences.LastSeenReleaseNotesVersion
    };
}
