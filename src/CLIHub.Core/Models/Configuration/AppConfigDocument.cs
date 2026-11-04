namespace CLIHub.Core.Models;

using CLIHub.Core.Configuration;

/// <summary>
///   Persistence representation of the existing flat config.json document.
/// </summary>
internal sealed class AppConfigDocument
{
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
            Projects = snapshot.Projects.Select(ProjectDocument.From).ToList(),
            Preferences = PreferencesDocument.From(snapshot.Preferences),
            CurrentProjectId = snapshot.CurrentProjectId
        };
    }

    /// <summary>
    ///   Creates the public configuration model from the persistence document.
    /// </summary>
    /// <returns> The public configuration model. </returns>
    public AppConfig ToAppConfig() => this.ToSnapshot().ToAppConfig();

    /// <summary>
    ///   Creates detached configuration state from the persistence document.
    /// </summary>
    /// <returns> Detached configuration state. </returns>
    public ConfigurationSnapshot ToSnapshot() => new()
    {
        Projects = (this.Projects ?? []).Select(project => project.ToProject()).ToList(),
        Preferences = (this.Preferences ?? new PreferencesDocument()).ToPreferences(),
        CurrentProjectId = this.CurrentProjectId
    };

    internal sealed class ProjectDocument
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime LastUsed { get; set; }
        public string? LogoPath { get; set; }
        public string Source { get; set; } = ProjectSources.ManualToken;

        // Both From and ToProject must map every public property of Project;
        // PersistenceCloneRoundTripTests fails the suite when a field is missed.
        public static ProjectDocument From(Project project) => new()
        {
            Id = project.Id,
            Name = project.Name,
            Path = project.Path,
            IsFavorite = project.IsFavorite,
            LastUsed = project.LastUsed,
            LogoPath = project.LogoPath,
            Source = ProjectSources.ToToken(project.Source)
        };

        public Project ToProject() => new()
        {
            Id = this.Id,
            Name = this.Name,
            Path = this.Path,
            IsFavorite = this.IsFavorite,
            LastUsed = this.LastUsed,
            LogoPath = this.LogoPath,
            Source = ProjectSources.Parse(this.Source)
        };
    }

    internal sealed class PreferencesDocument
    {
        public bool StartWithWindows { get; set; }
        public string Hotkey { get; set; } = "Ctrl+Shift+Space";
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
        public bool StartMinimized { get; set; } = true;
        public bool PinLaunchWindow { get; set; }
        public string PopupScreenPosition { get; set; } = "center";
        public string AppTheme { get; set; } = "dark";
        public string AccentColor { get; set; } = "#4CC2FF";
        public string PathDisplayStyle { get; set; } = PathDisplayStyles.LeftTrimToken;
        public string? LastSeenReleaseNotesVersion { get; set; }
        public List<string>? WatchedDirectories { get; set; }
        public List<WatchedDirectorySettingDocument>? WatchedDirectorySettings { get; set; }
        public int ScanDepth { get; set; } = ProjectScanningDefaults.DefaultScanDepth;
        public List<string>? IgnoredPatterns { get; set; }
        public string DiscoveryMode { get; set; } = ProjectDiscoveryModes.BothToken;
        public string? DefaultAgentId { get; set; }
        public string? PluginFeedUrl { get; set; }

        // Both From and ToPreferences must map every public property of AppPreferences;
        // PersistenceCloneRoundTripTests fails the suite when a field is missed.
        public static PreferencesDocument From(AppPreferences preferences) => new()
        {
            StartWithWindows = preferences.StartWithWindows,
            Hotkey = preferences.Hotkey,
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
            PinLaunchWindow = preferences.PinLaunchWindow,
            PopupScreenPosition = PopupScreenPositions.ToToken(preferences.PopupScreenPosition),
            AppTheme = AppThemes.ToToken(preferences.AppTheme),
            AccentColor = AccentPalette.Normalize(preferences.AccentColor),
            PathDisplayStyle = preferences.PathDisplayStyle,
            LastSeenReleaseNotesVersion = preferences.LastSeenReleaseNotesVersion,
            WatchedDirectories = preferences.Scanning.WatchedDirectories is null ? null : new List<string>(preferences.Scanning.WatchedDirectories),
            WatchedDirectorySettings = preferences.Scanning.WatchedDirectorySettings is { Count: > 0 }
                ? preferences.Scanning.WatchedDirectorySettings
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                    .Select(entry => new WatchedDirectorySettingDocument
                    {
                        Path = entry.Key,
                        ScanDepth = entry.Value.ScanDepth,
                        DiscoveryMode = ProjectDiscoveryModes.ToToken(entry.Value.DiscoveryMode)
                    })
                    .ToList()
                : null,
            ScanDepth = preferences.Scanning.ScanDepth,
            IgnoredPatterns = preferences.Scanning.IgnoredPatterns is null ? null : new List<string>(preferences.Scanning.IgnoredPatterns),
            DiscoveryMode = ProjectDiscoveryModes.ToToken(preferences.Scanning.DiscoveryMode),
            DefaultAgentId = preferences.DefaultAgentId,
            PluginFeedUrl = preferences.PluginFeedUrl
        };

        public AppPreferences ToPreferences() => new()
        {
            StartWithWindows = this.StartWithWindows,
            Hotkey = this.Hotkey,
            DefaultRuntime = RuntimeKinds.Parse(this.DefaultRuntime),
            LogLevel = this.LogLevel,
            LogRetentionDays = this.LogRetentionDays,
            LogFileSizeLimitMb = this.LogFileSizeLimitMb,
            ShowOnlyProjectAgents = this.ShowOnlyProjectAgents,
            AgentProbeTtlMinutes = this.AgentProbeTtlMinutes,
            AgentProbeTimeoutSeconds = this.AgentProbeTimeoutSeconds,
            CheckForUpdatesOnStartup = this.CheckForUpdatesOnStartup,
            AutoDownloadUpdates = this.AutoDownloadUpdates,
            NotifyStableUpdates = this.NotifyStableUpdates,
            NotifyPrereleaseUpdates = this.NotifyPrereleaseUpdates,
            StartMinimized = this.StartMinimized,
            PinLaunchWindow = this.PinLaunchWindow,
            PopupScreenPosition = PopupScreenPositions.Parse(this.PopupScreenPosition),
            AppTheme = AppThemes.Parse(this.AppTheme),
            AccentColor = AccentPalette.Normalize(this.AccentColor),
            PathDisplayStyle = this.PathDisplayStyle,
            LastSeenReleaseNotesVersion = this.LastSeenReleaseNotesVersion,
            Scanning = new ScanningPreferences
            {
                WatchedDirectories = this.WatchedDirectories is null ? null : new List<string>(this.WatchedDirectories),
                WatchedDirectorySettings = BuildWatchedDirectorySettings(this.WatchedDirectorySettings, this.WatchedDirectories),
                ScanDepth = this.ScanDepth,
                IgnoredPatterns = this.IgnoredPatterns is null ? null : new List<string>(this.IgnoredPatterns),
                DiscoveryMode = ProjectDiscoveryModes.Parse(this.DiscoveryMode)
            },
            DefaultAgentId = this.DefaultAgentId,
            PluginFeedUrl = this.PluginFeedUrl
        };

        private static IReadOnlyDictionary<string, WatchedDirectoryScanSettings> BuildWatchedDirectorySettings(
            List<WatchedDirectorySettingDocument>? entries,
            List<string>? watchedDirectories)
        {
            var result = new Dictionary<string, WatchedDirectoryScanSettings>(StringComparer.OrdinalIgnoreCase);

            if (entries is null || watchedDirectories is null || watchedDirectories.Count == 0)
            {
                return result;
            }

            var roots = new HashSet<string>(watchedDirectories, StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.Path) || !roots.Contains(entry.Path))
                {
                    continue;
                }

                result[entry.Path] = new WatchedDirectoryScanSettings(
                    ProjectScanningDefaults.NormalizeScanDepth(entry.ScanDepth),
                    ProjectDiscoveryModes.Parse(entry.DiscoveryMode));
            }

            return result;
        }
    }

    internal sealed class WatchedDirectorySettingDocument
    {
        public string Path { get; set; } = string.Empty;
        public int ScanDepth { get; set; } = ProjectScanningDefaults.DefaultScanDepth;
        public string DiscoveryMode { get; set; } = ProjectDiscoveryModes.BothToken;
    }
}
