namespace CLIHub.Tests.Models;

using System.Text.Json;

using CLIHub.Core.Models;
using CLIHub.Core.Services;

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
}
