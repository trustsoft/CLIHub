namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Plugins;

public class PluginDescriptorValidatorTests
{
    [Fact]
    public void Validate_ValidPlugin_ReturnsValidResult()
    {
        var result = new PluginDescriptorValidator().Validate(CreatePlugin());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_MissingId_ReturnsIdError()
    {
        var plugin = CreatePlugin();
        plugin.Id = string.Empty;

        var result = new PluginDescriptorValidator().Validate(plugin);

        Assert.Equal(["Plugin ID is required"], result.Errors);
    }

    [Fact]
    public void Validate_MissingName_ReturnsNameError()
    {
        var plugin = CreatePlugin();
        plugin.Name = string.Empty;

        var result = new PluginDescriptorValidator().Validate(plugin);

        Assert.Equal(["Plugin Name is required"], result.Errors);
    }

    [Fact]
    public void Validate_MissingLaunchCommand_ReturnsLaunchError()
    {
        var plugin = CreatePlugin();
        plugin.Commands.Launch = null;

        var result = new PluginDescriptorValidator().Validate(plugin);

        Assert.Equal(["Plugin must define a launch command"], result.Errors);
    }

    [Fact]
    public void Validate_MultipleErrors_PreservesRuleOrder()
    {
        var plugin = CreatePlugin();
        plugin.Id = string.Empty;
        plugin.Name = string.Empty;
        plugin.Commands.Launch = null;

        var result = new PluginDescriptorValidator().Validate(plugin);

        Assert.Equal(
            ["Plugin ID is required", "Plugin Name is required", "Plugin must define a launch command"],
            result.Errors);
    }

    private static Plugin CreatePlugin() => new()
    {
        Id = "plugin",
        Name = "Plugin",
        Commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = "plugin" }
        }
    };
}
