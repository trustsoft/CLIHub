namespace CLIHub.Core.Plugins;

/// <summary>
///   Reads and deserializes plugin.json descriptors.
/// </summary>
public interface IPluginDescriptorReader
{
    /// <summary>
    ///   Reads the <c> plugin.json </c> descriptor in the supplied directory.
    /// </summary>
    /// <param name="pluginDirectory"> The plugin directory to inspect. </param>
    /// <returns> The parsed plugin or the descriptor read error. </returns>
    PluginDescriptorReadResult Read(string pluginDirectory);

    /// <summary>
    ///   Reads a <c> plugin.json </c> descriptor from its raw JSON content, without touching the
    ///   file system. Package inspection uses this seam to validate an in-memory descriptor
    ///   before any file is written.
    /// </summary>
    /// <param name="json"> The raw descriptor JSON text. </param>
    /// <returns> The parsed plugin or the descriptor read error. </returns>
    PluginDescriptorReadResult ReadContent(string json);
}
