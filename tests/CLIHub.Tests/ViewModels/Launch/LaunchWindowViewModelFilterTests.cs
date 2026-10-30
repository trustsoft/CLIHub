namespace CLIHub.ViewModels;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Tests.Builders;

public class LaunchWindowViewModelFilterTests
{
    [Fact]
    public void ToggleFilterAction_UpdatesShowOnlyProjectAgents_PersistsPreference_RefreshesAgents()
    {
        var detection = InstalledDetection();
        var plugin = new Plugin { Id = "agent", Name = "Agent" };
        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .WithPlugins(plugin)
            .WithAgentDetectionService(detection)
            .BuildWithMocks();
        var preferencesStore = mocks.PreferencesStore;
        var filterAction = viewModel.AgentsActions.Single(action => action.IsCheckable);
        Action<AppPreferences>? update = null;
        preferencesStore.Invocations.Clear();
        preferencesStore.Setup(store => store.Update(It.IsAny<Action<AppPreferences>>()))
            .Callback<Action<AppPreferences>>(callback => update = callback);
        detection.Invocations.Clear();

        filterAction.IsChecked = true;

        Assert.True(viewModel.ShowOnlyProjectAgents);
        preferencesStore.Verify(store => store.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
        var persisted = new AppPreferences();
        update!(persisted);
        Assert.True(persisted.ShowOnlyProjectAgents);
        Assert.Equal(1, detection.Invocations.Count(invocation => invocation.Method.Name == nameof(IAgentDetectionService.IsInstalledInSystem)));
    }

    [Fact]
    public void SetShowOnlyProjectAgents_MirrorsToFilterAction()
    {
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithPreferences(new AppPreferences { ShowOnlyProjectAgents = false })
            .Build();

        viewModel.ShowOnlyProjectAgents = true;

        var filterAction = viewModel.AgentsActions.Single(action => action.IsCheckable);
        Assert.True(filterAction.IsChecked);
    }

    [Fact]
    public void Constructor_SeedsShowOnlyProjectAgents_DoesNotPersist()
    {
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(store => store.Load()).Returns(new AppPreferences { ShowOnlyProjectAgents = true });
        var (viewModel, _) = new LaunchWindowViewModelBuilder()
            .WithPreferencesStore(preferencesStore)
            .BuildWithMocks();

        var filterAction = viewModel.AgentsActions.Single(action => action.IsCheckable);

        Assert.True(viewModel.ShowOnlyProjectAgents);
        Assert.True(filterAction.IsChecked);
        preferencesStore.Verify(store => store.Update(It.IsAny<Action<AppPreferences>>()), Times.Never);
    }

    [Fact]
    public void ToggleFilterAction_OnePersistenceCall_OneRefresh()
    {
        var detection = InstalledDetection();
        var (viewModel, mocks) = new LaunchWindowViewModelBuilder()
            .WithPlugins(new Plugin { Id = "agent", Name = "Agent" })
            .WithAgentDetectionService(detection)
            .BuildWithMocks();
        var preferencesStore = mocks.PreferencesStore;
        var filterAction = viewModel.AgentsActions.Single(action => action.IsCheckable);
        preferencesStore.Invocations.Clear();
        detection.Invocations.Clear();

        filterAction.IsChecked = true;

        preferencesStore.Verify(store => store.Update(It.IsAny<Action<AppPreferences>>()), Times.Once);
        Assert.Equal(1, detection.Invocations.Count(invocation => invocation.Method.Name == nameof(IAgentDetectionService.IsInstalledInSystem)));
    }

    private static Mock<IAgentDetectionService> InstalledDetection()
    {
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(service => service.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        return detection;
    }
}
