namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class EmbeddedPluginResourcesTests
{
    private static readonly string[] ExpectedBuiltinIds = ["opencode", "pi", "qwen-code"];

    [Fact]
    public void BuiltinPluginIds_EnumeratesTheThreeBuiltInAgents()
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

    [Fact]
    public void BuiltinPluginIds_NoLongerContainsOpenClaude()
    {
        // Act
        var ids = EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly);

        // Assert
        Assert.DoesNotContain("openclaude", ids);
    }

    [Fact]
    public void BuiltinPluginIds_NoLongerContainsGithubCopilot()
    {
        // Act
        var ids = EmbeddedPluginResources.BuiltinPluginIds(typeof(PluginSeeder).Assembly);

        // Assert
        Assert.DoesNotContain("github-copilot", ids);
    }
}
