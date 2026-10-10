namespace CLIHub.Core.Models;

using CLIHub.Core.Configuration;

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
    public const int CurrentSchemaVersion = 2;

    /// <summary>
    ///   The schema version represented by documents without version metadata.
    /// </summary>
    public const int LegacySchemaVersion = 0;

    /// <summary>
    ///   Projects stored in the configuration document.
    /// </summary>
    public List<ProjectDocument> Projects { get; set; } = new();

    /// <summary>
    ///   User preferences stored in the configuration document.
    /// </summary>
    public PreferencesDocument Preferences { get; set; } = new();

    /// <summary>
    ///   ID of the current project stored in the configuration document.
    /// </summary>
    public string? CurrentProjectId { get; set; }

    /// <summary>
    ///   Creates a persistence document from the public configuration model.
    /// </summary>
    /// <param name="config"> The configuration to represent. </param>
    /// <returns> The persistence representation. </returns>
    public static AppConfigDocument From(AppConfig config) => FromSnapshot(ConfigurationSnapshot.From(config));

    /// <summary>
    ///   Creates a persistence document from detached configuration state.
    /// </summary>
    /// <param name="snapshot"> The state to represent. </param>
    /// <returns> The persistence representation. </returns>
    public static AppConfigDocument FromSnapshot(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new AppConfigDocument
        {
            SchemaVersion = CurrentSchemaVersion,
            Projects = snapshot.Projects.Select(ProjectDocument.From).ToList(),
            Preferences = PreferencesDocument.From(snapshot.Preferences),
            CurrentProjectId = snapshot.CurrentProjectId
        };
    }

    /// <summary>
    ///   Creates the public configuration model from the persistence document.
    /// </summary>
    /// <returns> The public configuration model. </returns>
    public AppConfig ToAppConfig() => ToSnapshot().ToAppConfig();

    /// <summary>
    ///   Creates detached configuration state from the persistence document.
    /// </summary>
    /// <returns> Detached configuration state. </returns>
    public ConfigurationSnapshot ToSnapshot() => new()
    {
        Projects = (Projects ?? []).Select(project => project.ToProject()).ToList(),
        Preferences = (Preferences ?? new PreferencesDocument()).ToPreferences(),
        CurrentProjectId = CurrentProjectId
    };

    internal sealed class ProjectDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime LastUsed { get; set; }
        public string? LogoPath { get; set; }

        public static ProjectDocument From(Project project) => new()
        {
            Id = project.Id,
            Name = project.Name,
            Path = project.Path,
            IsFavorite = project.IsFavorite,
            LastUsed = project.LastUsed,
            LogoPath = project.LogoPath
        };

        public Project ToProject() => new()
        {
            Id = Id,
            Name = Name,
            Path = Path,
            IsFavorite = IsFavorite,
            LastUsed = LastUsed,
            LogoPath = LogoPath
        };
    }

    internal sealed class PreferencesDocument
    {
        public bool StartWithWindows { get; set; }
        public string Hotkey { get; set; } = "Ctrl+Shift+A";
        public string TerminalExecutable { get; set; } = "wt.exe";
        public string DefaultRuntime { get; set; } = "ps";
        public string LogLevel { get; set; } = "Information";
        public int LogRetentionDays { get; set; } = 7;
        public int LogFileSizeLimitMb { get; set; }
        public bool ShowOnlyProjectAgents { get; set; }
        public int? AgentProbeTtlMinutes { get; set; }
        public int? AgentProbeTimeoutSeconds { get; set; }
        public bool CheckForUpdatesOnStartup { get; set; } = true;
        public bool AutoDownloadUpdates { get; set; } = true;
        public bool NotifyStableUpdates { get; set; } = true;
        public bool NotifyPrereleaseUpdates { get; set; }
        public bool ShowWindowOnStartup { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
        public bool PinLaunchWindow { get; set; }
        public string PathDisplayStyle { get; set; } = PathDisplayStyles.LeftTrimToken;
        public string? LastSeenReleaseNotesVersion { get; set; }

        public static PreferencesDocument From(AppPreferences preferences) => new()
        {
            StartWithWindows = preferences.StartWithWindows,
            Hotkey = preferences.Hotkey,
            TerminalExecutable = preferences.TerminalExecutable,
            DefaultRuntime = RuntimeKinds.ToToken(preferences.DefaultRuntime),
            LogLevel = preferences.LogLevel,
            LogRetentionDays = preferences.LogRetentionDays,
            LogFileSizeLimitMb = preferences.LogFileSizeLimitMb,
            ShowOnlyProjectAgents = preferences.ShowOnlyProjectAgents,
            AgentProbeTtlMinutes = preferences.AgentProbeTtlMinutes,
            AgentProbeTimeoutSeconds = preferences.AgentProbeTimeoutSeconds,
            CheckForUpdatesOnStartup = preferences.CheckForUpdatesOnStartup,
            AutoDownloadUpdates = preferences.AutoDownloadUpdates,
            NotifyStableUpdates = preferences.NotifyStableUpdates,
            NotifyPrereleaseUpdates = preferences.NotifyPrereleaseUpdates,
            StartMinimized = preferences.StartMinimized,
            // Keep the legacy field non-stale so older readers stay consistent.
            ShowWindowOnStartup = !preferences.StartMinimized,
            PinLaunchWindow = preferences.PinLaunchWindow,
            PathDisplayStyle = preferences.PathDisplayStyle,
            LastSeenReleaseNotesVersion = preferences.LastSeenReleaseNotesVersion
        };

        public AppPreferences ToPreferences() => new()
        {
            StartWithWindows = StartWithWindows,
            Hotkey = Hotkey,
            TerminalExecutable = TerminalExecutable,
            DefaultRuntime = RuntimeKinds.Parse(DefaultRuntime),
            LogLevel = LogLevel,
            LogRetentionDays = LogRetentionDays,
            LogFileSizeLimitMb = LogFileSizeLimitMb,
            ShowOnlyProjectAgents = ShowOnlyProjectAgents,
            AgentProbeTtlMinutes = AgentProbeTtlMinutes,
            AgentProbeTimeoutSeconds = AgentProbeTimeoutSeconds,
            CheckForUpdatesOnStartup = CheckForUpdatesOnStartup,
            AutoDownloadUpdates = AutoDownloadUpdates,
            NotifyStableUpdates = NotifyStableUpdates,
            NotifyPrereleaseUpdates = NotifyPrereleaseUpdates,
            StartMinimized = StartMinimized,
            ShowWindowOnStartup = ShowWindowOnStartup,
            PinLaunchWindow = PinLaunchWindow,
            PathDisplayStyle = PathDisplayStyle,
            LastSeenReleaseNotesVersion = LastSeenReleaseNotesVersion
        };
    }
}
