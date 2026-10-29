namespace CLIHub.Core.Tests.Configuration;

using System.Reflection;
using System.Text.Json;

using CLIHub.Core.Models;
using CLIHub.Core.Configuration;

public class PersistenceCloneRoundTripTests
{
    // The property lists that the round-trip tests below assert, per model. The census test ties
    // them to all public properties, so a future property fails the suite until it is added here
    // and to the assertions in the round-trip tests.
    private static readonly string[] CoveredProjectProperties =
    [
        nameof(Project.Id),
        nameof(Project.Name),
        nameof(Project.Path),
        nameof(Project.Source),
        nameof(Project.IsFavorite),
        nameof(Project.LastUsed),
        nameof(Project.LogoPath)
    ];

    private static readonly string[] CoveredPreferenceProperties =
    [
        nameof(AppPreferences.StartWithWindows),
        nameof(AppPreferences.Hotkey),
        nameof(AppPreferences.TerminalExecutable),
        nameof(AppPreferences.DefaultRuntime),
        nameof(AppPreferences.LogLevel),
        nameof(AppPreferences.LogRetentionDays),
        nameof(AppPreferences.LogFileSizeLimitMb),
        nameof(AppPreferences.ShowOnlyProjectAgents),
        nameof(AppPreferences.AgentProbeTtlMinutes),
        nameof(AppPreferences.AgentProbeTimeoutSeconds),
        nameof(AppPreferences.CheckForUpdatesOnStartup),
        nameof(AppPreferences.AutoDownloadUpdates),
        nameof(AppPreferences.NotifyStableUpdates),
        nameof(AppPreferences.NotifyPrereleaseUpdates),
        nameof(AppPreferences.ShowWindowOnStartup),
        nameof(AppPreferences.StartMinimized),
        nameof(AppPreferences.PinLaunchWindow),
        nameof(AppPreferences.PopupScreenPosition),
        nameof(AppPreferences.AppTheme),
        nameof(AppPreferences.AccentColor),
        nameof(AppPreferences.PathDisplayStyle),
        nameof(AppPreferences.LastSeenReleaseNotesVersion),
        nameof(AppPreferences.Scanning),
        nameof(AppPreferences.DefaultAgentId)
    ];

    [Fact]
    public void ProjectDocument_RoundTrip_EveryProjectPropertySurvives()
    {
        var config = CreateFullConfig();

        var restored = RoundTripThroughDocument(config);

        AssertProjectPropertiesMatch(config.Projects[0], restored.Projects[0]);
    }

    [Fact]
    public void PreferencesDocument_RoundTrip_EveryPreferencePropertySurvives()
    {
        var config = CreateFullConfig();

        var restored = RoundTripThroughDocument(config);

        AssertPreferencesPropertiesMatch(config.Preferences, restored.Preferences);
    }

    [Fact]
    public void ConfigurationSnapshot_FromToAppConfig_RoundTripsEveryProjectAndPreferenceProperty()
    {
        var config = CreateFullConfig();

        var restored = ConfigurationSnapshot.From(config).ToAppConfig();

        AssertProjectPropertiesMatch(config.Projects[0], restored.Projects[0]);
        AssertPreferencesPropertiesMatch(config.Preferences, restored.Preferences);
        Assert.Equal(config.CurrentProjectId, restored.CurrentProjectId);
    }

    [Fact]
    public void ConfigurationSnapshot_Clone_RoundTripsEveryProjectAndPreferenceProperty()
    {
        var config = CreateFullConfig();

        var clone = ConfigurationSnapshot.From(config).Clone();

        AssertProjectPropertiesMatch(config.Projects[0], clone.Projects[0]);
        AssertPreferencesPropertiesMatch(config.Preferences, clone.Preferences);
        Assert.Equal(config.CurrentProjectId, clone.CurrentProjectId);
    }

    [Fact]
    public void ConfigurationSnapshot_Clone_MutatingNestedCollectionsDoesNotChangeSource()
    {
        var config = CreateFullConfig();
        var snapshot = ConfigurationSnapshot.From(config);

        var clone = snapshot.Clone();
        clone.Projects[0].Name = "Mutated";
        clone.Projects[0].IsFavorite = false;
        clone.Preferences.Hotkey = "Ctrl+Alt+M";
        clone.Preferences.Scanning.WatchedDirectories!.Add("E:\\New");
        clone.Preferences.Scanning.IgnoredPatterns!.Add("extra");
        var cloneSettings = Assert.IsType<Dictionary<string, WatchedDirectoryScanSettings>>(clone.Preferences.Scanning.WatchedDirectorySettings);
        cloneSettings["C:\\Work"] = new WatchedDirectoryScanSettings(8, ProjectDiscoveryMode.Agents);
        cloneSettings["E:\\New"] = new WatchedDirectoryScanSettings(2, ProjectDiscoveryMode.Both);

        Assert.Equal("Project", snapshot.Projects[0].Name);
        Assert.True(snapshot.Projects[0].IsFavorite);
        Assert.Equal("Ctrl+Alt+P", snapshot.Preferences.Hotkey);
        Assert.Equal(["C:\\Work", "D:\\Deep"], snapshot.Preferences.Scanning.WatchedDirectories);
        Assert.Equal(["node_modules", "*cache*"], snapshot.Preferences.Scanning.IgnoredPatterns);
        Assert.Equal(4, snapshot.Preferences.Scanning.WatchedDirectorySettings["C:\\Work"].ScanDepth);
        Assert.Equal(ProjectDiscoveryMode.Git, snapshot.Preferences.Scanning.WatchedDirectorySettings["C:\\Work"].DiscoveryMode);
        Assert.False(snapshot.Preferences.Scanning.WatchedDirectorySettings.ContainsKey("E:\\New"));
    }

    [Fact]
    public void ScanningPreferences_Copy_DetachesNestedCollections()
    {
        var source = new ScanningPreferences
        {
            WatchedDirectories = ["C:\\Work"],
            WatchedDirectorySettings = new Dictionary<string, WatchedDirectoryScanSettings>
            {
                ["C:\\Work"] = new(4, ProjectDiscoveryMode.Git)
            },
            IgnoredPatterns = ["node_modules"]
        };

        var copy = source.Copy();
        copy.WatchedDirectories!.Add("D:\\Deep");
        var copiedSettings = Assert.IsType<Dictionary<string, WatchedDirectoryScanSettings>>(copy.WatchedDirectorySettings);
        copiedSettings["D:\\Deep"] = new(2, ProjectDiscoveryMode.Both);

        Assert.Equal(["C:\\Work"], source.WatchedDirectories);
        Assert.False(source.WatchedDirectorySettings.ContainsKey("D:\\Deep"));
    }

    [Fact]
    public void RoundTripCensus_CoveredPropertyLists_MatchAllPublicPropertiesOfProjectAndAppPreferences()
    {
        var projectProperties = typeof(Project)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var preferenceProperties = typeof(AppPreferences)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(projectProperties, CoveredProjectProperties.OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Assert.Equal(preferenceProperties, CoveredPreferenceProperties.OrderBy(name => name, StringComparer.Ordinal).ToArray());
    }

    private static AppConfig CreateFullConfig()
    {
        var config = new AppConfig
        {
            Projects =
            {
                new Project
                {
                    Id = "project-1",
                    Name = "Project",
                    Path = "C:\\Project",
                    Source = ProjectSource.Discovered,
                    IsFavorite = true,
                    LastUsed = new DateTime(2026, 10, 8, 12, 30, 0, DateTimeKind.Utc),
                    LogoPath = "logo.png"
                }
            },
            CurrentProjectId = "project-1",
            Preferences = new AppPreferences
            {
                StartWithWindows = true,
                Hotkey = "Ctrl+Alt+P",
                TerminalExecutable = "terminal.exe",
                DefaultRuntime = RuntimeKind.GitBash,
                LogLevel = "Debug",
                LogRetentionDays = 14,
                LogFileSizeLimitMb = 50,
                ShowOnlyProjectAgents = true,
                AgentProbeTtlMinutes = 15,
                AgentProbeTimeoutSeconds = 7,
                CheckForUpdatesOnStartup = false,
                AutoDownloadUpdates = false,
                NotifyStableUpdates = false,
                NotifyPrereleaseUpdates = true,
                StartMinimized = false,
                ShowWindowOnStartup = true,
                PinLaunchWindow = true,
                PopupScreenPosition = PopupScreenPosition.TopCenter,
                AppTheme = AppTheme.System,
                AccentColor = "#FFB900",
                PathDisplayStyle = "middleEllipsis",
                LastSeenReleaseNotesVersion = "1.2.3",
                Scanning = new ScanningPreferences
                {
                    WatchedDirectories = ["C:\\Work", "D:\\Deep"],
                    WatchedDirectorySettings = new Dictionary<string, WatchedDirectoryScanSettings>
                    {
                        ["C:\\Work"] = new(4, ProjectDiscoveryMode.Git),
                        ["D:\\Deep"] = new(7, ProjectDiscoveryMode.Agents)
                    },
                    ScanDepth = 5,
                    IgnoredPatterns = ["node_modules", "*cache*"],
                    DiscoveryMode = ProjectDiscoveryMode.Agents
                },
                DefaultAgentId = "opencode"
            }
        };
        return config;
    }

    private static AppConfig RoundTripThroughDocument(AppConfig config) =>
        JsonSerializer.Deserialize<AppConfigDocument>(
            JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options),
            CoreJson.Options)!.ToAppConfig();

    private static void AssertProjectPropertiesMatch(Project expected, Project actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Path, actual.Path);
        Assert.Equal(expected.Source, actual.Source);
        Assert.Equal(expected.IsFavorite, actual.IsFavorite);
        Assert.Equal(expected.LastUsed, actual.LastUsed);
        Assert.Equal(expected.LogoPath, actual.LogoPath);
    }

    private static void AssertPreferencesPropertiesMatch(AppPreferences expected, AppPreferences actual)
    {
        Assert.Equal(expected.StartWithWindows, actual.StartWithWindows);
        Assert.Equal(expected.Hotkey, actual.Hotkey);
        Assert.Equal(expected.TerminalExecutable, actual.TerminalExecutable);
        Assert.Equal(expected.DefaultRuntime, actual.DefaultRuntime);
        Assert.Equal(expected.LogLevel, actual.LogLevel);
        Assert.Equal(expected.LogRetentionDays, actual.LogRetentionDays);
        Assert.Equal(expected.LogFileSizeLimitMb, actual.LogFileSizeLimitMb);
        Assert.Equal(expected.ShowOnlyProjectAgents, actual.ShowOnlyProjectAgents);
        Assert.Equal(expected.AgentProbeTtlMinutes, actual.AgentProbeTtlMinutes);
        Assert.Equal(expected.AgentProbeTimeoutSeconds, actual.AgentProbeTimeoutSeconds);
        Assert.Equal(expected.CheckForUpdatesOnStartup, actual.CheckForUpdatesOnStartup);
        Assert.Equal(expected.AutoDownloadUpdates, actual.AutoDownloadUpdates);
        Assert.Equal(expected.NotifyStableUpdates, actual.NotifyStableUpdates);
        Assert.Equal(expected.NotifyPrereleaseUpdates, actual.NotifyPrereleaseUpdates);
        // ShowWindowOnStartup is derived on write at the document boundary (stored as
        // !StartMinimized); the source is kept consistent so the derived relation holds on the
        // snapshot boundary (verbatim copy) and the document boundary alike.
        Assert.Equal(!expected.StartMinimized, actual.ShowWindowOnStartup);
        Assert.Equal(expected.StartMinimized, actual.StartMinimized);
        Assert.Equal(expected.PinLaunchWindow, actual.PinLaunchWindow);
        Assert.Equal(expected.PopupScreenPosition, actual.PopupScreenPosition);
        Assert.Equal(expected.AppTheme, actual.AppTheme);
        Assert.Equal(expected.AccentColor, actual.AccentColor);
        Assert.Equal(expected.PathDisplayStyle, actual.PathDisplayStyle);
        Assert.Equal(expected.LastSeenReleaseNotesVersion, actual.LastSeenReleaseNotesVersion);
        Assert.Equal(expected.Scanning.WatchedDirectories, actual.Scanning.WatchedDirectories);
        Assert.Equal(expected.Scanning.WatchedDirectorySettings.Count, actual.Scanning.WatchedDirectorySettings.Count);
        foreach (var entry in expected.Scanning.WatchedDirectorySettings)
        {
            Assert.True(actual.Scanning.WatchedDirectorySettings.TryGetValue(entry.Key, out var actualSettings));
            Assert.Equal(entry.Value, actualSettings);
        }

        Assert.Equal(expected.Scanning.ScanDepth, actual.Scanning.ScanDepth);
        Assert.Equal(expected.Scanning.IgnoredPatterns, actual.Scanning.IgnoredPatterns);
        Assert.Equal(expected.Scanning.DiscoveryMode, actual.Scanning.DiscoveryMode);
        Assert.Equal(expected.DefaultAgentId, actual.DefaultAgentId);
    }
}
