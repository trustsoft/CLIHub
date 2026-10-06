namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Validates the required fields of plugin descriptors.
/// </summary>
public sealed class PluginDescriptorValidator : IPluginDescriptorValidator
{
    /// <inheritdoc />
    public PluginValidationResult Validate(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(plugin.Id))
        {
            errors.Add("Plugin ID is required");
        }

        if (string.IsNullOrWhiteSpace(plugin.Name))
        {
            errors.Add("Plugin Name is required");
        }

        if (plugin.Commands?.Launch == null)
        {
            errors.Add("Plugin must define a launch command");
        }

        return new PluginValidationResult(errors);
    }
}
