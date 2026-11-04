namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class EmbeddedPluginResourcesTests
{
    private static readonly string[] ExpectedBuiltinIds = ["opencode", "pi", "github-copilot", "openclaude", "qwen-code"];

    [Fact]
    public void BuiltinPluginIds_EnumeratesTheFiveBuiltInAgents()
    {
        // Act
        var ids = EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly);

        // Assert
        Assert.Equal(
            ExpectedBuiltinIds.OrderBy(id => id, StringComparer.Ordinal),
            ids.OrderBy(id => id, StringComparer.Ordinal));
    }

    [Fact]
    public void BuiltinPluginIds_NoLongerContainsClineCli()
    {
        // Act
        var ids = EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly);

        // Assert
        Assert.DoesNotContain("cline-cli", ids);
    }
}
