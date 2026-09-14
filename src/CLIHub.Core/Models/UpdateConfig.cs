using System.Text.Json.Serialization;

namespace CLIHub.Core.Models;

public sealed class UpdateConfig
{
    [JsonPropertyName("checkOnStartup")]
    public bool? CheckOnStartup { get; set; }

    [JsonIgnore]
    public bool EffectiveCheckOnStartup => CheckOnStartup ?? true;
}
