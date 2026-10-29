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

        if (plugin.SchemaVersionSpecified)
        {
            if (plugin.SchemaVersion is null)
            {
                errors.Add("Schema version must be an integer");
            }
            else if (plugin.SchemaVersion <= 0)
            {
                errors.Add("Schema version must be positive");
            }
            else if (plugin.SchemaVersion > 1)
            {
                errors.Add($"Unsupported schema version {plugin.SchemaVersion}; maximum supported is 1");
            }
        }

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
