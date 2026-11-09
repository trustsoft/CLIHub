namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Tests.Builders;

using Moq;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void UpdateCommand_WhenCanUpdateIsFalse_CannotExecute()
    {
        var a = Launchable("a", withUpdate: true);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");

        Assert.True(SpinWait.SpinUntil(
            () => card.UpdateAvailability?.Status == AgentUpdateStatus.Unknown,
            TimeSpan.FromSeconds(5)));

        Assert.False(card.CanUpdate);
        Assert.False(viewModel.UpdateCommand.CanExecute(card));
    }


    [Fact]
    public void UpdateCommand_WhenCanUpdateIsTrue_CanExecute()
    {
        var a = Launchable("a", withUpdate: true);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");

        Assert.True(SpinWait.SpinUntil(
            () => card.UpdateAvailability?.Status == AgentUpdateStatus.UpdateAvailable,
            TimeSpan.FromSeconds(5)));
        Assert.True(SpinWait.SpinUntil(
            () => !card.IsBlockedByRunningAgent || card.CanUpdate,
            TimeSpan.FromSeconds(5)));

        Assert.True(card.CanUpdate);
        Assert.True(viewModel.UpdateCommand.CanExecute(card));
    }


    [Fact]
    public void UpdateCommand_WhenCanUpdateChanges_RaisesCanExecuteChanged()
    {
        var a = Launchable("a", withUpdate: true);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.Null(card.UpdateAvailability);
        var raises = 0;
        viewModel.UpdateCommand.CanExecuteChanged += (_, _) => raises++;

        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0");

        Assert.True(raises > 0);
        Assert.True(viewModel.UpdateCommand.CanExecute(card));
    }


    [Fact]
    public void UpdateCommand_WhenStarted_ClearsUpdateAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"));
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithUpdate(update).Build();
        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(SpinWait.SpinUntil(() => card.CanUpdate, TimeSpan.FromSeconds(5)));

        viewModel.UpdateCommand.Execute(card);

        Assert.Null(card.UpdateAvailability);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.False(card.CanUpdate);
        Assert.Equal("Update started", card.Message);
    }

}
