using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class DetectSpec
{
    [JsonPropertyName("project")]
    public List<string>? Project { get; set; }
}
