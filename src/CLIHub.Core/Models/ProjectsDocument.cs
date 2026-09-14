using System.Text.Json;
using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class ProjectsDocument
{
    public const string FileName = "projects.json";

    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("projects")]
    public List<ProjectConfig> Projects { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? AdditionalData { get; set; }
}
