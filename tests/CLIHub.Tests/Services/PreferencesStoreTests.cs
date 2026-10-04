namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;

public class PreferencesStoreTests
{
    [Fact]
    public void Save_UpdatesPreferences_AndPreservesProjectState()
    {
        var project = new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project"
        };
        var config = new AppConfig
        {
            Projects = [project],
            CurrentProjectId = project.Id
        };
        var configService = new FakeConfigService(config);
        var store = new PreferencesStore(configService);
        var preferences = store.Load();
        preferences.Hotkey = "Ctrl+Alt+P";

        store.Save(preferences);

        Assert.Equal("Ctrl+Alt+P", config.Preferences.Hotkey);
        Assert.Single(config.Projects);
        Assert.Equal(project.Id, config.CurrentProjectId);
        Assert.Equal(1, configService.SaveCount);
    }
}
