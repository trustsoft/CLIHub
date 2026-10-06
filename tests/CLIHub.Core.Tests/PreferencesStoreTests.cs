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
        var repository = new FakeConfigurationRepository(config);
        var store = new PreferencesStore(repository);
        store.Update(preferences => preferences.Hotkey = "Ctrl+Alt+P");

        Assert.Equal("Ctrl+Alt+P", repository.Read().Preferences.Hotkey);
        Assert.Single(config.Projects);
        Assert.Equal(project.Id, config.CurrentProjectId);
        Assert.Equal(1, repository.UpdateCount);
    }

    [Fact]
    public void Update_CallbackFailure_DoesNotPersistPartialPreferences()
    {
        var repository = new FakeConfigurationRepository(new AppConfig
        {
            Preferences = new AppPreferences { Hotkey = "Ctrl+Alt+P" }
        });
        var store = new PreferencesStore(repository);

        Assert.Throws<InvalidOperationException>(() => store.Update(preferences =>
        {
            preferences.Hotkey = "Ctrl+Alt+M";
            throw new InvalidOperationException("cancel update");
        }));

        Assert.Equal("Ctrl+Alt+P", store.Load().Hotkey);
        Assert.Equal(0, repository.UpdateCount);
    }
}
