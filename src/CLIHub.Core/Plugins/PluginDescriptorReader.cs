namespace CLIHub.Core.Plugins;

using System.Text.Json;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Reads and deserializes plugin.json files.
/// </summary>
public sealed class PluginDescriptorReader : IPluginDescriptorReader
{
    /// <inheritdoc />
    public PluginDescriptorReadResult Read(string pluginDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginDirectory);

        var descriptorPath = Path.Combine(pluginDirectory, "plugin.json");
        if (!File.Exists(descriptorPath))
        {
            return new PluginDescriptorReadResult(null, null);
        }

        try
        {
            var json = File.ReadAllText(descriptorPath);
            var plugin = JsonSerializer.Deserialize<Plugin>(json, CoreJson.Options);
            return new PluginDescriptorReadResult(plugin, null);
        }
        catch (Exception ex)
        {
            return new PluginDescriptorReadResult(null, ex);
        }
    }
}
