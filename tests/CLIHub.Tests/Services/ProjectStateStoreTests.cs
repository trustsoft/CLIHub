namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;

public class ProjectStateStoreTests
{
    [Fact]
    public void Save_UpdatesProjectState_AndPreservesPreferences()
    {
        var config = new AppConfig
        {
            Preferences = new AppPreferences { Hotkey = "Ctrl+Alt+P" }
        };
        var configService = new FakeConfigService(config);
        var store = new ProjectStateStore(configService);
        var state = store.Load();
        state.CurrentProjectId = "project-1";
        state.Projects.Add(new Project
        {
            Id = "project-1",
            Name = "Project",
            Path = "C:\\Project"
        });

        store.Save(state);

        Assert.Equal("project-1", config.CurrentProjectId);
        Assert.Single(config.Projects);
        Assert.Equal("Ctrl+Alt+P", config.Preferences.Hotkey);
        Assert.Equal(1, configService.SaveCount);
    }
}
