namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Tests.Builders;

using Moq;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void UpdateCommand_WhenStarted_InvalidatesCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }


    [Fact]
    public void UpdateCommand_WhenNotStarted_KeepsCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Failed, "boom"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(It.IsAny<Plugin>()), Times.Never);
    }


    [Fact]
    public void UpdateCommand_ReportsOutcome()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).Build();
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update started", viewModel.Cards[0].Message);
    }


    [Fact]
    public void UpdateCommand_WhenServiceBlocks_SurfacesServiceMessage()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Blocked, "Agent is running — stop it to update"));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).Build();
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Agent is running — stop it to update", viewModel.Cards[0].Message);
    }

}
