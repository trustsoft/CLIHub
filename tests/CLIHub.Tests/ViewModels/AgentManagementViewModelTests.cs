namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.ViewModels;
using CLIHub.Lifetime;
using CLIHub.Launching;

public class AgentManagementViewModelTests
{
    private static Plugin Launchable(string id, bool withUpdate = false)
    {
        var plugin = new Plugin
        {
            Id = id,
            Name = id,
            PluginDirectory = $"C:\\plugins\\{id}",
            Commands = new AgentCommands { Launch = new PluginCommand { Executable = id } }
        };

        if (withUpdate)
        {
            plugin.Commands.Update = new PluginCommand { Executable = id, Arguments = "update" };
        }

        return plugin;
    }

    private static AgentManagementViewModel CreateViewModel(
        Plugin[] plugins,
        Mock<IAgentDetectionService> detection,
        Mock<IAgentVersionService> version,
        Mock<IAgentManagementService> management,
        Mock<IExternalLauncher> launcher)
    {
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        return new AgentManagementViewModel(
            catalog.Object,
            detection.Object,
            version.Object,
            management.Object,
            launcher.Object,
            new InlineOperationLifetime(),
            NullLogger<AgentManagementViewModel>.Instance);
    }

    private static Mock<IAgentDetectionService> Detection()
    {
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        return detection;
    }

    private static Mock<IAgentVersionService> Versions(string value = "1.0")
    {
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync(value);
        return version;
    }

    [Fact]
    public void Load_BuildsCardsMetricsAndDefaultOptions()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load("b");

        Assert.Equal(2, viewModel.Cards.Count);
        Assert.Equal("2 supported · 1 installed", viewModel.Metrics);
        Assert.Equal("b", viewModel.CurrentDefaultAgentId);
        Assert.Contains(viewModel.DefaultAgentOptions, option => option.Id is null);
        Assert.True(viewModel.Cards.Single(c => c.PluginId == "a").CanUpdate);
        Assert.False(viewModel.Cards.Single(c => c.PluginId == "b").CanUpdate);
    }

    [Fact]
    public void Load_PopulatesVersions()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions("3.4.5"), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("3.4.5", viewModel.Cards[0].Version);
    }

    [Fact]
    public void TestCommand_ProbeUpdatesCardAndMetrics()
    {
        var a = Launchable("a");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        var management = new Mock<IAgentManagementService>();
        management.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "9.9"));
        var viewModel = CreateViewModel([a], detection, Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);
        Assert.Equal("1 supported · 0 installed", viewModel.Metrics);

        viewModel.TestCommand.Execute(viewModel.Cards[0]);

        Assert.True(viewModel.Cards[0].IsInstalled);
        Assert.Equal("9.9", viewModel.Cards[0].Version);
        Assert.Equal("1 supported · 1 installed", viewModel.Metrics);
    }

    [Fact]
    public void UpdateCommand_ReportsOutcome()
    {
        var a = Launchable("a", withUpdate: true);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update started.", viewModel.Cards[0].Message);
    }

    [Fact]
    public void EditConfigCommand_OpensConfigPath()
    {
        var a = Launchable("a");
        var launcher = new Mock<IExternalLauncher>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), launcher);
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.EditConfigCommand.Execute(card);

        Assert.NotNull(card.ConfigPath);
        launcher.Verify(x => x.Open(card.ConfigPath!), Times.Once);
    }

    private sealed class InlineOperationLifetime : IApplicationOperationLifetime
    {
        public CancellationToken Token => CancellationToken.None;

        public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
        {
            var task = operation(CancellationToken.None);
            task.GetAwaiter().GetResult();
            return task;
        }

        public Task StopAsync(TimeSpan timeout) => Task.CompletedTask;
    }
}
