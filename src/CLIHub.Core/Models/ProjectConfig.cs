using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class ProjectConfig
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}
