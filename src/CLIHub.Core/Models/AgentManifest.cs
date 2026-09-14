using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class AgentManifest
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("actions")]
    public Dictionary<string, AgentActionSpec> Actions { get; set; } = new();

    [JsonPropertyName("detect")]
    public DetectSpec? Detect { get; set; }
}
