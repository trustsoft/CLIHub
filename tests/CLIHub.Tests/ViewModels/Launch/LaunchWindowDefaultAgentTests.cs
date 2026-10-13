namespace CLIHub.ViewModels;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Tests.Builders;

public class LaunchWindowDefaultAgentTests
{
    private static Plugin Launchable(string id) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands { Launch = new PluginCommand { Executable = id } }
    };

    private static Mock<IAgentDetectionService> InstalledDetection()
    {
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        return detection;
    }

    [Fact]
    public void ApplyDefaultAgentSelection_StoredAgentPresent_SelectsIt()
    {
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithPlugins(Launchable("a"), Launchable("b"))
            .WithPreferences(new AppPreferences { DefaultAgentId = "b" })
            .WithAgentDetectionService(InstalledDetection())
            .Build();

        viewModel.ApplyDefaultAgentSelection();

        Assert.Equal("b", viewModel.SelectedAgent?.Plugin.Id);
    }

    [Fact]
    public void ApplyDefaultAgentSelection_NoDefault_LeavesSelectionCleared()
    {
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithPlugins(Launchable("a"))
            .WithPreferences(new AppPreferences { DefaultAgentId = null })
            .WithAgentDetectionService(InstalledDetection())
            .Build();

        viewModel.ApplyDefaultAgentSelection();

        Assert.Null(viewModel.SelectedAgent);
    }

    [Fact]
    public void ApplyDefaultAgentSelection_UnknownDefault_LeavesSelectionCleared()
    {
        var viewModel = new LaunchWindowViewModelBuilder()
            .WithPlugins(Launchable("a"))
            .WithPreferences(new AppPreferences { DefaultAgentId = "missing" })
            .WithAgentDetectionService(InstalledDetection())
            .Build();

        viewModel.ApplyDefaultAgentSelection();

        Assert.Null(viewModel.SelectedAgent);
    }
}
