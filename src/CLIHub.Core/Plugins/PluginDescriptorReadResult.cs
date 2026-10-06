namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   Result of reading one plugin descriptor.
/// </summary>
/// <param name="Plugin"> The deserialized plugin, or null when no descriptor was present or it contained JSON null. </param>
/// <param name="Error"> The file or JSON deserialization error, or null when no error occurred. </param>
public sealed record PluginDescriptorReadResult(Plugin? Plugin, Exception? Error);
