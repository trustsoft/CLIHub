namespace CLIHub.Tests.Services;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

public class StartMinimizedMigrationTests
{
    private readonly StartMinimizedMigration _migration = new();

    [Fact]
    public void Migrate_ShowWindowEnabled_SetsStartMinimizedDisabled()
    {
        var snapshot = new ConfigurationSnapshot();
        snapshot.Preferences.ShowWindowOnStartup = true;

        var migrated = _migration.Migrate(snapshot);

        Assert.False(migrated.Preferences.StartMinimized);
    }

    [Fact]
    public void Migrate_ShowWindowDisabled_SetsStartMinimizedEnabled()
    {
        var snapshot = new ConfigurationSnapshot();
        snapshot.Preferences.ShowWindowOnStartup = false;

        var migrated = _migration.Migrate(snapshot);

        Assert.True(migrated.Preferences.StartMinimized);
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
        snapshot.Preferences.ShowWindowOnStartup = false;
        snapshot.Preferences.Hotkey = "Ctrl+Alt+P";

        var migrated = _migration.Migrate(snapshot);

        Assert.Equal(project.Id, migrated.CurrentProjectId);
        Assert.Equal(project.Name, migrated.Projects.Single().Name);
        Assert.Equal("Ctrl+Alt+P", migrated.Preferences.Hotkey);
    }
}
