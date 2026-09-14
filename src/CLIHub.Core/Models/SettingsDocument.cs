using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class SettingsDocument
{
    public const string FileName = "settings.json";
    public const string DefaultHotkey = "Ctrl+Alt+Space";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("runtime")]
    public string? Runtime { get; set; }

    [JsonPropertyName("hotkey")]
    public string Hotkey { get; set; } = DefaultHotkey;

    [JsonPropertyName("probe")]
    public ProbeConfig Probe { get; set; } = new();

    [JsonPropertyName("update")]
    public UpdateConfig Update { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
