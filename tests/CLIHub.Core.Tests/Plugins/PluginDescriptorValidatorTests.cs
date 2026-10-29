namespace CLIHub.Core.Tests.Plugins;

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

    [Fact]
    public void Validate_SchemaVersionAbsent_IsValid()
    {
        var result = new PluginDescriptorValidator().Validate(CreatePlugin());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_SchemaVersionOne_IsValid()
    {
        var result = new PluginDescriptorValidator().Validate(CreatePlugin(1));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_SchemaVersionTwo_IsUnsupported()
    {
        var result = new PluginDescriptorValidator().Validate(CreatePlugin(2));

        Assert.Equal(["Unsupported schema version 2; maximum supported is 1"], result.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveSchemaVersion_IsRejected(int schemaVersion)
    {
        var result = new PluginDescriptorValidator().Validate(CreatePlugin(schemaVersion));

        Assert.Equal(["Schema version must be positive"], result.Errors);
    }

    [Fact]
    public void Validate_NullSchemaVersionWhenExplicitlySpecified_IsRejectedAsTypeMismatch()
    {
        var plugin = CreatePlugin();
        plugin.SchemaVersion = null;

        var result = new PluginDescriptorValidator().Validate(plugin);

        Assert.Equal(["Schema version must be an integer"], result.Errors);
    }

    private static Plugin CreatePlugin(int? schemaVersion = null)
    {
        var plugin = new Plugin
        {
            Id = "plugin",
            Name = "Plugin",
            Commands = new AgentCommands
            {
                Launch = new PluginCommand { Executable = "plugin" }
            }
        };

        if (schemaVersion is not null)
        {
            plugin.SchemaVersion = schemaVersion;
        }

        return plugin;
    }
}
