using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class AgentsDocument
{
    public const string FileName = "agents.json";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("agents")]
    public Dictionary<string, AgentProbeEntry> Agents { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
