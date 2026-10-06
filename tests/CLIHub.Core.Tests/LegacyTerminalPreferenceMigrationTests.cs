namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class LegacyTerminalPreferenceMigrationTests
{
    private readonly LegacyTerminalPreferenceMigration _migration = new();

    [Theory]
    [InlineData("wt.exe", "wt")]
    [InlineData("cmd.exe", "cmd")]
    [InlineData("powershell.exe", "ps")]
    [InlineData("pwsh.exe", "ps")]
    public void Migrate_RecognizedExecutable_MapsToRuntimeToken(string executable, string expected)
    {
        var snapshot = new ConfigurationSnapshot();
        snapshot.Preferences.TerminalExecutable = executable;

        var migrated = _migration.Migrate(snapshot);

        Assert.Equal(expected, migrated.Preferences.DefaultRuntime);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-terminal.exe")]
    public void Migrate_MissingOrUnknownExecutable_UsesWindowsTerminal(string? executable)
    {
        var snapshot = new ConfigurationSnapshot();
        snapshot.Preferences.TerminalExecutable = executable ?? "wt.exe";
        if (executable is null)
        {
            snapshot.Preferences.TerminalExecutable = string.Empty;
        }

        var migrated = _migration.Migrate(snapshot);

        Assert.Equal(RuntimeKinds.WindowsTerminalToken, migrated.Preferences.DefaultRuntime);
    }

    [Fact]
    public void Migrate_PreservesUnrelatedState()
    {
        var project = new Project { Id = "project-1", Name = "Project", Path = "C:\\Project" };
        var snapshot = new ConfigurationSnapshot
        {
            Projects = [project],
            CurrentProjectId = project.Id
        };
        snapshot.Preferences.TerminalExecutable = "cmd.exe";
        snapshot.Preferences.Hotkey = "Ctrl+Alt+P";

        var migrated = _migration.Migrate(snapshot);

        Assert.Equal(project.Id, migrated.CurrentProjectId);
        Assert.Equal(project.Name, migrated.Projects.Single().Name);
        Assert.Equal("Ctrl+Alt+P", migrated.Preferences.Hotkey);
    }

    [Fact]
    public void ConfigurationRepository_LegacyDocument_UsesRegisteredTerminalMigration()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clihub-legacy-runtime-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
        {
          "projects": [],
          "preferences": { "terminalExecutable": "cmd.exe" },
          "currentProjectId": null
        }
        """);

        try
        {
            var runner = new ConfigMigrationRunner([new LegacyTerminalPreferenceMigration()]);
            using var repository = new ConfigurationRepository(
                NullLogger<ConfigurationRepository>.Instance,
                path,
                runner);

            Assert.Equal("cmd", repository.Read().Preferences.DefaultRuntime);
            repository.Flush();
            var persisted = File.ReadAllText(path);
            Assert.Contains("\"schemaVersion\": 1", persisted);
            Assert.Contains("\"defaultRuntime\": \"cmd\"", persisted);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ConfigurationRepository_CurrentDocument_DoesNotApplyLegacyMigration()
    {
        var path = Path.Combine(Path.GetTempPath(), $"clihub-current-runtime-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """
        {
          "schemaVersion": 1,
          "projects": [],
          "preferences": { "terminalExecutable": "cmd.exe", "defaultRuntime": "wt" },
          "currentProjectId": null
        }
        """);

        try
        {
            var runner = new ConfigMigrationRunner([new LegacyTerminalPreferenceMigration()]);
            using var repository = new ConfigurationRepository(
                NullLogger<ConfigurationRepository>.Instance,
                path,
                runner);

            Assert.Equal("wt", repository.Read().Preferences.DefaultRuntime);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
