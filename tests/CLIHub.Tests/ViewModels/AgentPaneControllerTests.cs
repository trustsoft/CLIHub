namespace CLIHub.Tests.ViewModels;

using Moq;

using CLIHub;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.ViewModels;

public class AgentPaneControllerTests
{
    [Fact]
    public void Refresh_PreservesSelectedRowIdentityAndAppliesFilter()
    {
        var first = Plugin("first");
        var second = Plugin("second");
        var catalog = new Mock<IPluginCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetAllPlugins()).Returns([first, second]);
        var detection = new Mock<IAgentDetectionService>(MockBehavior.Strict);
        detection.Setup(x => x.Invalidate());
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detection
            .Setup(x => x.IsAvailableInProject(It.IsAny<Plugin>(), "C:\\Project"))
            .Returns((Plugin plugin, string _) => plugin.Id == "first");
        var lifetime = CreateLifetime();
        var controller = new AgentPaneController(
            catalog.Object,
            detection.Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            lifetime.Object);

        Assert.True(controller.Refresh("C:\\Project", false));
        var selected = controller.Agents[0];
        controller.Select(selected);

        Assert.True(controller.Refresh("C:\\Project", true));

        Assert.Single(controller.Agents);
        Assert.Same(selected, controller.Agents[0]);
        Assert.Same(selected, controller.SelectedAgent);
    }

    [Fact]
    public void InvalidateCaches_ForwardsToDetectionVersionAndLogoServices()
    {
        var detection = new Mock<IAgentDetectionService>(MockBehavior.Strict);
        detection.Setup(x => x.Invalidate());
        var versions = new Mock<IAgentVersionService>(MockBehavior.Strict);
        versions.Setup(x => x.Invalidate());
        var logos = new Mock<ILogoCacheService>(MockBehavior.Strict);
        logos.Setup(x => x.InvalidateAll());
        var controller = new AgentPaneController(
            new Mock<IPluginCatalog>().Object,
            detection.Object,
            versions.Object,
            logos.Object,
            CreateLifetime().Object);

        controller.InvalidateCaches();

        detection.Verify(x => x.Invalidate(), Times.Once);
        versions.Verify(x => x.Invalidate(), Times.Once);
        logos.Verify(x => x.InvalidateAll(), Times.Once);
    }

    [Fact]
    public void PluginsChanged_InvalidatesCachesAndRefreshesWithLastContext()
    {
        var first = Plugin("first");
        var second = Plugin("second");
        IReadOnlyList<Plugin> plugins = [first, second];
        var catalog = new Mock<IPluginCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetAllPlugins()).Returns(() => plugins);
        var detection = new Mock<IAgentDetectionService>(MockBehavior.Strict);
        detection.Setup(x => x.Invalidate());
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detection.Setup(x => x.IsAvailableInProject(It.IsAny<Plugin>(), "C:\\Project")).Returns(true);
        var versions = new Mock<IAgentVersionService>(MockBehavior.Strict);
        versions.Setup(x => x.Invalidate());
        var logos = new Mock<ILogoCacheService>(MockBehavior.Strict);
        logos.Setup(x => x.InvalidateAll());
        var controller = new AgentPaneController(
            catalog.Object,
            detection.Object,
            versions.Object,
            logos.Object,
            CreateLifetime().Object);

        controller.Refresh("C:\\Project", true);
        var selected = controller.Agents[0];
        controller.Select(selected);
        plugins = [second];

        catalog.Raise(x => x.PluginsChanged += null, EventArgs.Empty);

        Assert.Single(controller.Agents);
        Assert.Equal("second", controller.Agents[0].Plugin.Id);
        Assert.Null(controller.SelectedAgent);
        versions.Verify(x => x.Invalidate(), Times.Once);
        logos.Verify(x => x.InvalidateAll(), Times.Once);
        detection.Verify(x => x.Invalidate(), Times.Once);
    }

    [Fact]
    public void Dispose_UnsubscribesAndCancelsVersionPopulation()
    {
        var catalog = new Mock<IPluginCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetAllPlugins()).Returns([Plugin("agent")]);
        var detection = new Mock<IAgentDetectionService>(MockBehavior.Strict);
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detection.Setup(x => x.Invalidate());
        var lifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        lifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns(Task.CompletedTask);
        var controller = new AgentPaneController(
            catalog.Object,
            detection.Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            lifetime.Object);
        controller.Refresh(null, false);

        controller.Dispose();
        catalog.Raise(x => x.PluginsChanged += null, EventArgs.Empty);

        detection.Verify(x => x.Invalidate(), Times.Never);
        lifetime.Verify(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()), Times.Once);
        Assert.False(controller.Refresh(null, false));
    }

    [Fact]
    public void RefreshCommand_ExecutesRefreshWithCurrentContext()
    {
        var first = Plugin("first");
        var catalog = new Mock<IPluginCatalog>(MockBehavior.Strict);
        catalog.Setup(x => x.GetAllPlugins()).Returns([first]);
        var detection = new Mock<IAgentDetectionService>(MockBehavior.Strict);
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        detection.Setup(x => x.IsAvailableInProject(first, "C:\\Project")).Returns(true);
        var lifetime = CreateLifetime();
        var controller = new AgentPaneController(
            catalog.Object,
            detection.Object,
            new Mock<IAgentVersionService>().Object,
            new Mock<ILogoCacheService>().Object,
            lifetime.Object);

        controller.Refresh("C:\\Project", true);
        controller.RefreshCommand.Execute(null);

        Assert.Single(controller.Agents);
        Assert.Equal("first", controller.Agents[0].Plugin.Id);
    }

    private static Mock<IApplicationOperationLifetime> CreateLifetime()
    {
        var lifetime = new Mock<IApplicationOperationLifetime>(MockBehavior.Strict);
        lifetime
            .Setup(x => x.RunAsync(It.IsAny<string>(), It.IsAny<Func<CancellationToken, Task>>()))
            .Returns(Task.CompletedTask);
        return lifetime;
    }

    private static Plugin Plugin(string id) => new()
    {
        Id = id,
        Name = id,
        Detection = new AgentDetection()
    };
}
