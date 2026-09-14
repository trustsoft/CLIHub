using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class AgentActionSpec
{
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("runtime")]
    public string? Runtime { get; set; }
}
