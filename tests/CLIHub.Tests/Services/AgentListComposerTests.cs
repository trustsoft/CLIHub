namespace CLIHub.Tests.Services;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

using Moq;

public class AgentListComposerTests
{
    private readonly Mock<IAgentDetectionService> _detection = new();

    [Fact]
    public void Compose_AgentNotInstalled_ExcludesAgent()
    {
        var plugin = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(plugin))
            .Returns(false);

        var result = AgentListComposer.Compose([plugin], _detection.Object, null, onlyProjectAgents: false);

        Assert.Empty(result);
    }

    [Fact]
    public void Compose_AgentNotInstalled_WithProjectFilterOn_ExcludesAgent()
    {
        var plugin = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(plugin))
            .Returns(false);

        var result = AgentListComposer.Compose([plugin], _detection.Object, @"C:\proj", onlyProjectAgents: true);

        Assert.Empty(result);
        _detection.Verify(d => d.IsAvailableInProject(It.IsAny<Plugin>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Compose_AgentInstalled_NoProjectSelected_ListsAgentAsAvailable()
    {
        var plugin = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(plugin))
            .Returns(true);

        var result = AgentListComposer.Compose([plugin], _detection.Object, null, onlyProjectAgents: false);

        var entry = Assert.Single(result);
        Assert.Same(plugin, entry.Plugin);
        Assert.True(entry.IsAvailable);
        _detection.Verify(d => d.IsAvailableInProject(It.IsAny<Plugin>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Compose_AgentInstalled_UnavailableInProject_ListsAgentDimmed()
    {
        var plugin = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(plugin))
            .Returns(true);
        _detection
            .Setup(d => d.IsAvailableInProject(plugin, @"C:\proj"))
            .Returns(false);

        var result = AgentListComposer.Compose([plugin], _detection.Object, @"C:\proj", onlyProjectAgents: false);

        var entry = Assert.Single(result);
        Assert.Same(plugin, entry.Plugin);
        Assert.False(entry.IsAvailable);
    }

    [Fact]
    public void Compose_AgentInstalled_AvailableInProject_ListsAgentAsAvailable()
    {
        var plugin = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(plugin))
            .Returns(true);
        _detection
            .Setup(d => d.IsAvailableInProject(plugin, @"C:\proj"))
            .Returns(true);

        var result = AgentListComposer.Compose([plugin], _detection.Object, @"C:\proj", onlyProjectAgents: true);

        var entry = Assert.Single(result);
        Assert.Same(plugin, entry.Plugin);
        Assert.True(entry.IsAvailable);
    }

    [Fact]
    public void Compose_InstalledUnavailableInProjectAndUninstalled_WithProjectFilterOn_ReturnsEmptyList()
    {
        var installed = CreatePlugin("claude");
        var uninstalled = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(installed))
            .Returns(true);
        _detection
            .Setup(d => d.IsAvailableInProject(installed, @"C:\proj"))
            .Returns(false);
        _detection
            .Setup(d => d.IsInstalledInSystem(uninstalled))
            .Returns(false);

        var result = AgentListComposer.Compose([installed, uninstalled], _detection.Object, @"C:\proj", onlyProjectAgents: true);

        Assert.Empty(result);
    }

    [Fact]
    public void Compose_NoAgentsInstalled_ReturnsEmptyList()
    {
        var first = CreatePlugin("claude");
        var second = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Returns(false);

        var result = AgentListComposer.Compose([first, second], _detection.Object, @"C:\proj", onlyProjectAgents: true);

        Assert.Empty(result);
    }

    [Fact]
    public void Compose_MixedInstallationAndAvailability_KeepsPluginOrder()
    {
        var first = CreatePlugin("claude");
        var second = CreatePlugin("qwen");

        _detection
            .Setup(d => d.IsInstalledInSystem(first))
            .Returns(true);
        _detection
            .Setup(d => d.IsInstalledInSystem(second))
            .Returns(true);

        var result = AgentListComposer.Compose([first, second], _detection.Object, null, onlyProjectAgents: false);

        Assert.Equal(2, result.Count);
        Assert.Same(first, result[0].Plugin);
        Assert.Same(second, result[1].Plugin);
    }

    private static Plugin CreatePlugin(string id) => new()
    {
        Id = id,
        Name = id,
        Detection = new AgentDetection()
    };
}
