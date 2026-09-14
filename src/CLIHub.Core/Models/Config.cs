using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class Config
{
    public const string DefaultHotkey = "Ctrl+Alt+Space";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("runtime")]
    public string? Runtime { get; set; }

    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = DefaultHotkey;

    [JsonPropertyName("projects")]
    public List<ProjectConfig> Projects { get; set; } = new();

    [JsonPropertyName("probe")]
    public ProbeConfig Probe { get; set; } = new();

    [JsonPropertyName("agents")]
    public Dictionary<string, AgentProbeEntry> Agents { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
