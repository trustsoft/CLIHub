namespace CLIHub.Tests.Models;

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

        Assert.Contains("\"schemaVersion\": 1", json);
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
