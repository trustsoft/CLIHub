namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Configuration;
using CLIHub.Core.Projects;

public class ProjectStateStoreTests
{
    [Fact]
    public void Update_UpdatesProjectState_AndPreservesPreferences()
    {
        var config = new AppConfig
        {
            Preferences = new AppPreferences { Hotkey = "Ctrl+Alt+P" }
        };
        var configService = new FakeConfigService(config);
        var store = new ProjectStateStore(configService);
        store.Update(state =>
        {
            state.CurrentProjectId = "project-1";
            state.Projects.Add(new Project
            {
                Id = "project-1",
                Name = "Project",
                Path = "C:\\Project"
            });
        });

        var saved = configService.Load();
        Assert.Equal("project-1", saved.CurrentProjectId);
        Assert.Single(saved.Projects);
        Assert.Equal("Ctrl+Alt+P", saved.Preferences.Hotkey);
        Assert.Equal(1, configService.SaveCount);
    }

    [Fact]
    public void Update_CallbackFailure_DoesNotPersistPartialProjectState()
    {
        var configService = new FakeConfigService();
        var store = new ProjectStateStore(configService);

        Assert.Throws<InvalidOperationException>(() => store.Update(state =>
        {
            state.CurrentProjectId = "project-1";
            throw new InvalidOperationException("cancel update");
        }));

        Assert.Null(store.Load().CurrentProjectId);
        Assert.Equal(0, configService.SaveCount);
    }
}
