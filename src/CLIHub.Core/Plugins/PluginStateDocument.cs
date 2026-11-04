namespace CLIHub.Core.Plugins;

/// <summary>
///   The root document of <c> plugin-state.json </c>: a schema version plus the per-plugin
///   lifecycle entries keyed by plugin id.
/// </summary>
public sealed record PluginStateDocument
{
    /// <summary>
    ///   The state file schema version. The only supported value is 1.
    /// </summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>
    ///   The per-plugin lifecycle entries, keyed by plugin id with ordinal comparison.
    /// </summary>
    public Dictionary<string, PluginStateEntry> Plugins { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    ///   Creates an empty state document at the current schema version.
    /// </summary>
    /// <returns> A document with no plugin entries. </returns>
    public static PluginStateDocument Empty() => new();
}
