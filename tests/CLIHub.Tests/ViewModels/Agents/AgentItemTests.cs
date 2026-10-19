namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

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
            AgentCommandKind.Update,
            AgentCommandKind.Version);
        var item = new AgentItem { Plugin = plugin, Name = "Test" };

        Assert.True(item.CanLaunch);
        Assert.True(item.CanResume);
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
        Assert.False(item.CanUpdate);
        Assert.False(item.CanShowVersion);
    }

    [Fact]
    public void Update_PluginDeclaresDifferentCommands_RaisesCapabilityNotifications()
    {
        var item = new AgentItem { Plugin = CreatePluginWithCommands(AgentCommandKind.Launch), Name = "Test" };
        var notifications = TrackNotifications(item);

        item.Update(
            CreatePluginWithCommands(
                AgentCommandKind.Launch,
                AgentCommandKind.Resume,
                AgentCommandKind.Version,
                AgentCommandKind.Update),
            "Test",
            null,
            true);

        Assert.Contains(nameof(AgentItem.CanLaunch), notifications);
        Assert.Contains(nameof(AgentItem.CanResume), notifications);
        Assert.Contains(nameof(AgentItem.CanUpdate), notifications);
        Assert.Contains(nameof(AgentItem.CanShowVersion), notifications);
        Assert.True(item.CanResume);
        Assert.True(item.CanUpdate);
        Assert.True(item.CanShowVersion);
    }

    [Fact]
    public void Update_WithUnchangedValues_RaisesNoNotifications()
    {
        var plugin = CreatePluginWithCommands(AgentCommandKind.Launch);
        var item = new AgentItem { Plugin = plugin, Name = "Test", LogoPath = "logo.png", IsAvailable = true };
        var notifications = TrackNotifications(item);

        item.Update(plugin, "Test", "logo.png", true);

        Assert.Empty(notifications);
    }

    [Fact]
    public void Update_WithChangedValues_RaisesRowPropertyNotifications()
    {
        var item = new AgentItem { Plugin = CreatePluginWithCommands(AgentCommandKind.Launch), Name = "Test" };
        var notifications = TrackNotifications(item);

        item.Update(CreatePluginWithCommands(AgentCommandKind.Launch), "Renamed", "logo.png", false);

        Assert.Contains(nameof(AgentItem.Plugin), notifications);
        Assert.Contains(nameof(AgentItem.Name), notifications);
        Assert.Contains(nameof(AgentItem.LogoPath), notifications);
        Assert.Contains(nameof(AgentItem.IsAvailable), notifications);
        Assert.Contains(nameof(AgentItem.RowOpacity), notifications);
        Assert.Equal("Renamed", item.Name);
        Assert.Equal(0.4, item.RowOpacity);

        notifications.Clear();
        item.Update(item.Plugin, "Renamed", "logo.png", false);

        Assert.Empty(notifications);
    }

    private static List<string> TrackNotifications(AgentItem item)
    {
        var notifications = new List<string>();
        item.PropertyChanged += (_, e) => notifications.Add(e.PropertyName ?? string.Empty);
        return notifications;
    }

    private static Plugin CreatePluginWithCommands(params AgentCommandKind[] kinds)
    {
        var commands = new AgentCommands
        {
            Launch = kinds.Contains(AgentCommandKind.Launch) ? new PluginCommand { Executable = "test" } : null,
            Resume = kinds.Contains(AgentCommandKind.Resume) ? new PluginCommand { Executable = "test" } : null,
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
