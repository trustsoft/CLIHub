namespace CLIHub.Core.Plugins;

using System.Text.Json;
using System.Text.Json.Nodes;

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
            JsonObject? document;
            try
            {
                document = JsonNode.Parse(json)?.AsObject();
            }
            catch (JsonException)
            {
                document = null;
            }
            var hasSchemaVersion = document?.ContainsKey("schemaVersion") == true;
            var schemaVersion = document?["schemaVersion"];
            var invalidSchemaType = hasSchemaVersion
                && (schemaVersion is null
                    || schemaVersion.GetValueKind() != JsonValueKind.Number
                    || !schemaVersion.AsValue().TryGetValue<int>(out _));
            if (invalidSchemaType)
            {
                document!.Remove("schemaVersion");
                var pluginWithInvalidSchema = JsonSerializer.Deserialize<Plugin>(document.ToJsonString(), CoreJson.Options);
                if (pluginWithInvalidSchema is not null)
                {
                    pluginWithInvalidSchema.SchemaVersion = null;
                }

                return new PluginDescriptorReadResult(pluginWithInvalidSchema, null);
            }

            var plugin = JsonSerializer.Deserialize<Plugin>(json, CoreJson.Options);
            return new PluginDescriptorReadResult(plugin, null);
        }
        catch (Exception ex)
        {
            return new PluginDescriptorReadResult(null, ex);
        }
    }
}
