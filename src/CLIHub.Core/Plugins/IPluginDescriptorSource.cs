namespace CLIHub.Core.Plugins;

/// <summary>
///   Enumerates plugin descriptors from a source.
/// </summary>
public interface IPluginDescriptorSource
{
    /// <summary>
    ///   Enumerates descriptor entries available from the source.
    /// </summary>
    /// <returns> The descriptor entries. </returns>
    IEnumerable<PluginDescriptorSourceEntry> EnumerateDescriptors();
}
