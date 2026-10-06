namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Validates the required fields of a plugin descriptor.
/// </summary>
public interface IPluginDescriptorValidator
{
    /// <summary>
    ///   Validates one plugin descriptor.
    /// </summary>
    /// <param name="plugin"> The plugin descriptor to validate. </param>
    /// <returns> The structured validation result. </returns>
    PluginValidationResult Validate(Plugin plugin);
}
