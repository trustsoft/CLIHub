using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class AgentProbeEntry
{
    [JsonPropertyName("hostInstalled")]
    public bool HostInstalled { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("lastProbed")]
    public DateTimeOffset? LastProbed { get; set; }
}
