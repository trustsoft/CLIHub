using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class ProbeConfig
{
    public const int DefaultTtlMinutes = 1440;
    public const int DefaultTimeoutSeconds = 10;

    [JsonPropertyName("ttlMinutes")]
    public int? TtlMinutes { get; set; }

    [JsonPropertyName("timeoutSeconds")]
    public int? TimeoutSeconds { get; set; }

    [JsonIgnore]
    public int EffectiveTtlMinutes => TtlMinutes is > 0 ? TtlMinutes.Value : DefaultTtlMinutes;

    [JsonIgnore]
    public int EffectiveTimeoutSeconds => TimeoutSeconds is > 0 ? TimeoutSeconds.Value : DefaultTimeoutSeconds;
}
