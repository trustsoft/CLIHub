namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Models;
using CLIHub.ViewModels;

/// <summary>
///   Tests for AgentItem capability detection.
/// </summary>
public sealed class AgentItemTests
{
    [Fact]
    public void CanLaunch_WhenLaunchCommandExists_ReturnsTrue()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Launch);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanLaunch);
    }

    [Fact]
    public void CanLaunch_WhenLaunchCommandMissing_ReturnsFalse()
    {
        var plugin = CreatePluginWithCommands();
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanLaunch);
    }

    [Fact]
    public void CanResume_WhenResumeCommandExists_ReturnsTrue()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Resume);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanResume);
    }

    [Fact]
    public void CanResume_WhenResumeCommandMissing_ReturnsFalse()
    {
        var plugin = CreatePluginWithCommands();
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanResume);
    }

    [Fact]
    public void CanInit_WhenInitCommandExists_ReturnsTrue()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Init);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanInit);
    }

    [Fact]
    public void CanInit_WhenInitCommandMissing_ReturnsFalse()
    {
        var plugin = CreatePluginWithCommands();
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanInit);
    }

    [Fact]
    public void CanUpdate_WhenUpdateCommandExists_ReturnsTrue()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Update);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanUpdate);
    }

    [Fact]
    public void CanUpdate_WhenUpdateCommandMissing_ReturnsFalse()
    {
        var plugin = CreatePluginWithCommands();
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanUpdate);
    }

    [Fact]
    public void CanShowVersion_WhenVersionCommandExists_ReturnsTrue()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Version);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanShowVersion);
    }

    [Fact]
    public void CanShowVersion_WhenVersionCommandMissing_ReturnsFalse()
    {
        var plugin = CreatePluginWithCommands();
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanShowVersion);
    }

    [Fact]
    public void MultipleCapabilities_WhenMultipleCommandsExist_AllReturnTrue()
    {
        var plugin = CreatePluginWithCommands(
            AgentCommandKind.Launch,
            AgentCommandKind.Resume,
            AgentCommandKind.Init,
            AgentCommandKind.Update,
            AgentCommandKind.Version);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanLaunch);
        Assert.True(item.CanResume);
        Assert.True(item.CanInit);
        Assert.True(item.CanUpdate);
        Assert.True(item.CanShowVersion);
    }

    [Fact]
    public void Capabilities_WhenCommandsIsNull_AllReturnFalse()
    {
        var plugin = new Plugin
        {
            Id = "test-agent",
            Name = "Test Agent",
            Description = "Test",
            Commands = null!
        };
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.False(item.CanLaunch);
        Assert.False(item.CanResume);
        Assert.False(item.CanInit);
        Assert.False(item.CanUpdate);
        Assert.False(item.CanShowVersion);
    }

    private static Plugin CreatePluginWithCommands(params AgentCommandKind[] kinds)
    {
        var commands = new AgentCommands
        {
            Launch = kinds.Contains(AgentCommandKind.Launch) ? new PluginCommand { Executable = "test" } : null,
            Resume = kinds.Contains(AgentCommandKind.Resume) ? new PluginCommand { Executable = "test" } : null,
            Init = kinds.Contains(AgentCommandKind.Init) ? new PluginCommand { Executable = "test" } : null,
            Update = kinds.Contains(AgentCommandKind.Update) ? new PluginCommand { Executable = "test" } : null,
            Version = kinds.Contains(AgentCommandKind.Version) ? new PluginCommand { Executable = "test" } : null
        };

        return new Plugin
        {
            Id = "test-agent",
            Name = "Test Agent",
            Description = "Test",
            Commands = commands
        };
    }
}
