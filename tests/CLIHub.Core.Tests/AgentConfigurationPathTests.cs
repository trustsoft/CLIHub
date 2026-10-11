namespace CLIHub.Core.Tests.Agents;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

public class AgentConfigurationPathTests
{
    [Fact]
    public void For_PluginWithDirectory_ReturnsPluginJsonPath()
    {
        var plugin = new Plugin { Id = "opencode", Name = "OpenCode", PluginDirectory = "C:\\plugins\\opencode" };

        var path = AgentConfigurationPath.For(plugin);

        Assert.Equal(Path.Combine("C:\\plugins\\opencode", "plugin.json"), path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void For_PluginWithoutDirectory_ReturnsNull(string? directory)
    {
        var plugin = new Plugin { Id = "opencode", Name = "OpenCode", PluginDirectory = directory };

        var path = AgentConfigurationPath.For(plugin);

        Assert.Null(path);
    }
}
