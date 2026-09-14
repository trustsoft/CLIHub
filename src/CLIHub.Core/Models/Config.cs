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
}
