namespace CLIHub.Core.Plugins;

/// <summary>
///   A plugin whose on-disk descriptor no longer matches the hash of the content CLIHub last
///   wrote, recorded when the catalog verifies descriptors during load. The plugin still loads
///   normally; the mismatch marks it as user-modified. The recorded hashes are the evidence of
///   the modification, so the catalog never rewrites the stored hash when it detects one.
/// </summary>
/// <param name="PluginId"> The plugin id. </param>
/// <param name="PluginDirectory"> The plugin's directory on disk. </param>
/// <param name="Origin">
///   The recorded origin (<see cref="PluginOrigin" /> values), or null when the registry entry
///   carries none.
/// </param>
/// <param name="StoredContentHash"> The hash CLIHub last recorded for the descriptor, or null when none was recorded. </param>
/// <param name="DiskContentHash"> The hash of the descriptor currently on disk, or null when it could not be read. </param>
public sealed record UserModifiedPluginEntry(
    string PluginId,
    string PluginDirectory,
    string? Origin,
    string? StoredContentHash,
    string? DiskContentHash);
