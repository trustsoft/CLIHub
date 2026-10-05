namespace CLIHub.Core.Configuration;

using System.Text.Json;

/// <summary>
///   Shared serializer options for the JSON files Core services read and write: camelCase
///   property names with indentation.
/// </summary>
internal static class CoreJson
{
    /// <summary>
    ///   The single camelCase, indented <see cref="JsonSerializerOptions"/> used by Core services.
    /// </summary>
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
