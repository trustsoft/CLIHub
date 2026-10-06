namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Reads and deserializes a plugin descriptor from a plugin directory.
/// </summary>
public interface IPluginDescriptorReader
{
    /// <summary>
    ///   Reads the <c>plugin.json</c> descriptor in the supplied directory.
    /// </summary>
    /// <param name="pluginDirectory"> The plugin directory to inspect. </param>
    /// <returns> The parsed plugin or the descriptor read error. </returns>
    PluginDescriptorReadResult Read(string pluginDirectory);
}
