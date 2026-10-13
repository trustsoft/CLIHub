namespace CLIHub.Core.Tests.Configuration;

using System.Text.Json;

using CLIHub.Core.Models;
using CLIHub.Core.Configuration;

public class AppConfigSerializationTests
{
    [Fact]
    public void LastSeenReleaseNotesVersion_AbsentFromStoredJson_ReadsAsNull()
    {
        // A config.json written before the release-notes feature existed.
        const string json = """
        {
          "projects": [],
          "preferences": { "hotkey": "Ctrl+Shift+A", "showOnlyProjectAgents": false },
          "currentProjectId": null
        }
        """;

        var config = JsonSerializer.Deserialize<AppConfig>(json, CoreJson.Options);

        Assert.NotNull(config);
        Assert.Null(config!.Preferences.LastSeenReleaseNotesVersion);
    }

    [Fact]
    public void LastSeenReleaseNotesVersion_RoundTripsUnderItsCamelCaseKey()
    {
        var config = new AppConfig();
        config.Preferences.LastSeenReleaseNotesVersion = "0.5.0";

        var json = JsonSerializer.Serialize(config, CoreJson.Options);

        Assert.Contains("\"lastSeenReleaseNotesVersion\": \"0.5.0\"", json);

        var restored = JsonSerializer.Deserialize<AppConfig>(json, CoreJson.Options);

        Assert.Equal("0.5.0", restored!.Preferences.LastSeenReleaseNotesVersion);
    }

    [Fact]
    public void AppConfigDocument_PreservesTheExistingFlatDocumentShape()
    {
        var config = new AppConfig
        {
            Projects =
            {
                new Project { Id = "project-1", Name = "Project", Path = "C:\\Project" }
            },
            CurrentProjectId = "project-1"
        };

        var json = JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options);

        Assert.Contains("\"projects\": [", json);
        Assert.Contains("\"preferences\": {", json);
        Assert.Contains("\"currentProjectId\": \"project-1\"", json);

        var restored = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options)!.ToAppConfig();
        Assert.Single(restored.Projects);
        Assert.Equal("project-1", restored.CurrentProjectId);
    }

    [Fact]
    public void AppConfigDocument_NewWrites_IncludeCurrentSchemaVersion()
    {
        var json = JsonSerializer.Serialize(AppConfigDocument.From(new AppConfig()), CoreJson.Options);

        Assert.Contains("\"schemaVersion\": 2", json);
    }

    [Fact]
    public void AppConfigDocument_RoundTripsEveryConfigurationFieldThroughDtoMapping()
    {
        var project = new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project",
            IsFavorite = true,
            LastUsed = new DateTime(2026, 10, 8, 12, 30, 0, DateTimeKind.Utc),
            LogoPath = "logo.png"
        };
        var config = new AppConfig
        {
            Projects = [project],
            CurrentProjectId = project.Id,
            Preferences = new AppPreferences
            {
                StartWithWindows = true,
                Hotkey = "Ctrl+Alt+P",
                TerminalExecutable = "terminal.exe",
                DefaultRuntime = RuntimeKind.PowerShell,
                LogLevel = "Debug",
                ShowOnlyProjectAgents = true,
                AgentProbeTtlMinutes = 15,
                AgentProbeTimeoutSeconds = 7,
                CheckForUpdatesOnStartup = false,
                StartMinimized = true,
                PinLaunchWindow = true,
                PathDisplayStyle = "middleEllipsis",
                LastSeenReleaseNotesVersion = "1.2.3"
            }
        };

        var restored = JsonSerializer.Deserialize<AppConfigDocument>(
            JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options),
            CoreJson.Options)!.ToAppConfig();

        Assert.Equal(config.CurrentProjectId, restored.CurrentProjectId);
        Assert.Equal(config.Projects[0].Id, restored.Projects[0].Id);
        Assert.Equal(config.Projects[0].Name, restored.Projects[0].Name);
        Assert.Equal(config.Projects[0].Path, restored.Projects[0].Path);
        Assert.Equal(config.Projects[0].IsFavorite, restored.Projects[0].IsFavorite);
        Assert.Equal(config.Projects[0].LastUsed, restored.Projects[0].LastUsed);
        Assert.Equal(config.Projects[0].LogoPath, restored.Projects[0].LogoPath);
        Assert.Equal(config.Preferences.StartWithWindows, restored.Preferences.StartWithWindows);
        Assert.Equal(config.Preferences.Hotkey, restored.Preferences.Hotkey);
        Assert.Equal(config.Preferences.TerminalExecutable, restored.Preferences.TerminalExecutable);
        Assert.Equal(config.Preferences.DefaultRuntime, restored.Preferences.DefaultRuntime);
        Assert.Equal(config.Preferences.LogLevel, restored.Preferences.LogLevel);
        Assert.Equal(config.Preferences.ShowOnlyProjectAgents, restored.Preferences.ShowOnlyProjectAgents);
        Assert.Equal(config.Preferences.AgentProbeTtlMinutes, restored.Preferences.AgentProbeTtlMinutes);
        Assert.Equal(config.Preferences.AgentProbeTimeoutSeconds, restored.Preferences.AgentProbeTimeoutSeconds);
        Assert.Equal(config.Preferences.CheckForUpdatesOnStartup, restored.Preferences.CheckForUpdatesOnStartup);
        Assert.Equal(config.Preferences.StartMinimized, restored.Preferences.StartMinimized);
        Assert.Equal(config.Preferences.PinLaunchWindow, restored.Preferences.PinLaunchWindow);
        Assert.Equal(config.Preferences.PathDisplayStyle, restored.Preferences.PathDisplayStyle);
        Assert.Equal(config.Preferences.LastSeenReleaseNotesVersion, restored.Preferences.LastSeenReleaseNotesVersion);
    }

    [Fact]
    public void AppConfigDocument_UnknownRuntimeToken_UsesSafeDefault()
    {
        const string json = """
        {
          "schemaVersion": 1,
          "projects": [],
          "preferences": { "defaultRuntime": "future-runtime" },
          "currentProjectId": null
        }
        """;

        var document = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options)!;

        Assert.Equal(RuntimeKind.PowerShell, document.ToAppConfig().Preferences.DefaultRuntime);
    }

    [Fact]
    public void ProjectScanningPreferences_RoundTrip()
    {
        var config = new AppConfig();
        config.Preferences.WatchedDirectories = ["C:\\Work"];
        config.Preferences.ScanDepth = 4;
        config.Preferences.IgnoredPatterns = ["node_modules", "*cache*"];
        config.Preferences.DiscoveryMode = ProjectDiscoveryMode.Agents;

        var restored = JsonSerializer.Deserialize<AppConfigDocument>(
            JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options),
            CoreJson.Options)!.ToAppConfig();

        Assert.Equal(["C:\\Work"], restored.Preferences.WatchedDirectories);
        Assert.Equal(4, restored.Preferences.ScanDepth);
        Assert.Equal(["node_modules", "*cache*"], restored.Preferences.IgnoredPatterns);
        Assert.Equal(ProjectDiscoveryMode.Agents, restored.Preferences.DiscoveryMode);
    }

    [Fact]
    public void ProjectScanningPreferences_Absent_UseDefaults()
    {
        const string json = """
        { "schemaVersion": 2, "projects": [], "preferences": { "hotkey": "Ctrl+Shift+A" }, "currentProjectId": null }
        """;

        var config = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options)!.ToAppConfig();

        Assert.Null(config.Preferences.WatchedDirectories);
        Assert.Equal(ProjectScanningDefaults.DefaultScanDepth, config.Preferences.ScanDepth);
        Assert.Null(config.Preferences.IgnoredPatterns);
        Assert.Equal(ProjectDiscoveryMode.Both, config.Preferences.DiscoveryMode);
    }

    [Fact]
    public void DefaultAgentId_RoundTrips()
    {
        var config = new AppConfig();
        config.Preferences.DefaultAgentId = "opencode";

        var restored = JsonSerializer.Deserialize<AppConfigDocument>(
            JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options),
            CoreJson.Options)!.ToAppConfig();

        Assert.Equal("opencode", restored.Preferences.DefaultAgentId);
    }

    [Fact]
    public void DefaultAgentId_Absent_ReadsAsNull()
    {
        const string json = """
        { "schemaVersion": 2, "projects": [], "preferences": { "hotkey": "Ctrl+Shift+A" }, "currentProjectId": null }
        """;

        var config = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options)!.ToAppConfig();

        Assert.Null(config.Preferences.DefaultAgentId);
    }

    [Fact]
    public void ProjectSource_RoundTrips()
    {
        var config = new AppConfig
        {
            Projects = { new Project { Id = "p", Name = "p", Path = "C:\\p", Source = ProjectSource.Discovered } }
        };

        var restored = JsonSerializer.Deserialize<AppConfigDocument>(
            JsonSerializer.Serialize(AppConfigDocument.From(config), CoreJson.Options),
            CoreJson.Options)!.ToAppConfig();

        Assert.Equal(ProjectSource.Discovered, restored.Projects[0].Source);
    }

    [Fact]
    public void ProjectSource_Absent_ReadsAsManual()
    {
        const string json = """
        { "schemaVersion": 2, "projects": [ { "id": "p", "name": "p", "path": "C:\\p" } ], "preferences": {}, "currentProjectId": null }
        """;

        var config = JsonSerializer.Deserialize<AppConfigDocument>(json, CoreJson.Options)!.ToAppConfig();

        Assert.Equal(ProjectSource.Manual, config.Projects[0].Source);
    }

    [Fact]
    public void ProjectState_TransfersProjectOwnershipWithoutChangingValues()
    {
        var config = new AppConfig
        {
            Projects =
            {
                new Project { Id = "project-1", Name = "Project", Path = "C:\\Project" }
            },
            CurrentProjectId = "project-1"
        };

        var state = ProjectState.From(config);
        state.Projects[0].IsFavorite = true;
        var destination = new AppConfig();

        state.ApplyTo(destination);

        Assert.Equal("project-1", destination.CurrentProjectId);
        Assert.True(destination.Projects.Single().IsFavorite);
    }

    [Fact]
    public void ConfigurationSnapshot_RoundTripsNestedValuesWithoutSharingReferences()
    {
        var config = new AppConfig
        {
            Projects =
            {
                new Project { Id = "project-1", Name = "Project", Path = "C:\\Project" }
            },
            CurrentProjectId = "project-1"
        };
        config.Preferences.Hotkey = "Ctrl+Alt+P";

        var snapshot = ConfigurationSnapshot.From(config);
        snapshot.Projects[0].Name = "Changed";
        snapshot.Preferences.Hotkey = "Ctrl+Alt+M";

        Assert.Equal("Project", config.Projects[0].Name);
        Assert.Equal("Ctrl+Alt+P", config.Preferences.Hotkey);
        Assert.Equal("Changed", snapshot.ToAppConfig().Projects[0].Name);
    }
}
