namespace CLIHub.Core.Plugins;

/// <summary>
///   Result of a feed index fetch operation.
/// </summary>
/// <param name="Success"> Whether the feed was successfully fetched and parsed. </param>
/// <param name="Index"> The parsed feed index, or null on failure. </param>
/// <param name="Errors"> Error messages, if any. </param>
public sealed record PluginFeedResult(
    bool Success,
    PluginFeedIndex? Index,
    IReadOnlyList<string> Errors);

/// <summary>
///   Parsed plugin feed index.
/// </summary>
/// <param name="SchemaVersion"> Feed schema version. </param>
/// <param name="LastUpdated"> Optional informational timestamp. </param>
/// <param name="Plugins"> List of available plugins. </param>
public sealed record PluginFeedIndex(
    int SchemaVersion,
    string? LastUpdated,
    IReadOnlyList<PluginFeedEntry> Plugins);

/// <summary>
///   A single plugin entry from the feed index.
/// </summary>
/// <param name="Id"> Plugin ID. </param>
/// <param name="Name"> Plugin name. </param>
/// <param name="Description"> Plugin description. </param>
/// <param name="Version"> Plugin version. </param>
/// <param name="SchemaVersion"> Plugin descriptor schema version. </param>
/// <param name="MinAppVersion"> Minimum CLIHub version required. </param>
/// <param name="Publisher"> Plugin publisher. </param>
/// <param name="Homepage"> Plugin homepage URL. </param>
/// <param name="PackageUrl"> HTTPS URL to the plugin package. </param>
/// <param name="Sha256"> Hex-encoded SHA-256 checksum of the package. </param>
/// <param name="ReleaseNotesUrl"> Optional release notes URL. </param>
/// <param name="Signature"> Reserved for future signing; always null in this change. </param>
public sealed record PluginFeedEntry(
    string Id,
    string Name,
    string Description,
    string Version,
    int SchemaVersion,
    string MinAppVersion,
    string Publisher,
    string Homepage,
    string PackageUrl,
    string Sha256,
    string? ReleaseNotesUrl,
    string? Signature);

/// <summary>
///   Result of a package download operation.
/// </summary>
/// <param name="Success"> Whether the package was successfully downloaded and verified. </param>
/// <param name="TempFilePath"> Path to the temporary package file, or null on failure. </param>
/// <param name="Errors"> Error messages, if any. </param>
public sealed record PluginDownloadResult(
    bool Success,
    string? TempFilePath,
    IReadOnlyList<string> Errors);
