namespace CLIHub.Core.Plugins;

using CLIHub.Core.Models;

/// <summary>
///   A descriptor discovered by a plugin source.
/// </summary>
/// <param name="PluginDirectory"> The directory containing the descriptor. </param>
/// <param name="Plugin"> The loaded plugin, when reading succeeded. </param>
/// <param name="ReadError"> The error encountered while reading, if any. </param>
public sealed record PluginDescriptorSourceEntry(string PluginDirectory, Plugin? Plugin, Exception? ReadError);
