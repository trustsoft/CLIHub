namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Configuration;

public class PreferencesStoreTests
{
    [Fact]
    public void Update_UpdatesPreferences_AndPreservesProjectState()
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
        store.Update(preferences => preferences.Hotkey = "Ctrl+Alt+P");

        Assert.Equal("Ctrl+Alt+P", configService.Load().Preferences.Hotkey);
        Assert.Single(config.Projects);
        Assert.Equal(project.Id, config.CurrentProjectId);
        Assert.Equal(1, configService.SaveCount);
    }

    [Fact]
    public void Update_CallbackFailure_DoesNotPersistPartialPreferences()
    {
        var configService = new FakeConfigService(new AppConfig
        {
            Preferences = new AppPreferences { Hotkey = "Ctrl+Alt+P" }
        });
        var store = new PreferencesStore(configService);

        Assert.Throws<InvalidOperationException>(() => store.Update(preferences =>
        {
            preferences.Hotkey = "Ctrl+Alt+M";
            throw new InvalidOperationException("cancel update");
        }));

        Assert.Equal("Ctrl+Alt+P", store.Load().Hotkey);
        Assert.Equal(0, configService.SaveCount);
    }
}
