namespace CLIHub.Core.Plugins;

/// <summary>
///   The root document of <c> registry.json </c>: the per-plugin registry entries keyed by
///   plugin id. Tolerant reads ignore unknown fields, so the document carries no schema-version
///   field.
/// </summary>
public sealed record PluginRegistryDocument
{
    /// <summary>
    ///   The per-plugin registry entries, keyed by plugin id with ordinal comparison.
    /// </summary>
    public Dictionary<string, PluginRegistryEntry> Plugins { get; set; } = new(StringComparer.Ordinal);

    /// <summary>
    ///   Creates an empty registry document.
    /// </summary>
    /// <returns> A document with no plugin entries. </returns>
    public static PluginRegistryDocument Empty() => new();
}
