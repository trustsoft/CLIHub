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
    IReadOnlyList<string> Errors)
{
    /// <summary>
    ///   Whether the index was served from the last-good cache because the live fetch failed.
    ///   False for a fresh 200 response and for a 304 Not Modified (a 304 confirms the cached
    ///   content is still current, it does not fall back to it).
    /// </summary>
    public bool FromCache { get; init; }

    /// <summary>
    ///   When the cached index was stored, set only when <see cref="FromCache" /> is true; null
    ///   otherwise, so the UI can show the age of genuinely stale data.
    /// </summary>
    public DateTimeOffset? CachedAt { get; init; }

    /// <summary>
    ///   Whether the index passed signature verification or needs none. True for a verified default
    ///   feed, for a custom feed URL (verification is not applicable — the user chose the operator),
    ///   and for the cached state a 304 or a cache fallback serves (that state was verified before it
    ///   was cached). False only when a signature check failed; the result then carries the error and
    ///   no index.
    /// </summary>
    public bool SignatureValid { get; init; } = true;

    /// <summary>
    ///   The transition-window notice when the official feed served no signature and the index was
    ///   accepted anyway (missing signature before <c> SignatureRequiredAfter </c>); null otherwise.
    /// </summary>
    public string? SignatureWarning { get; init; }
}

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
///   A single plugin entry from the feed index, normalized across index schema versions 1 and 2.
///   v1 entries keep their package fields and synthesize <see cref="Compatibility" /> as
///   <c> "&gt;=" + MinAppVersion </c>; v2 entries carry the raw compatibility range, the inline
///   descriptor JSON, an optional package, channel, and logo URL.
/// </summary>
/// <param name="Id"> Plugin ID. </param>
/// <param name="Name"> Plugin name. </param>
/// <param name="Description"> Plugin description. </param>
/// <param name="Version"> Plugin version. </param>
/// <param name="SchemaVersion">
///   Descriptor schema version: the per-entry value for v1 entries; for v2 entries the inline
///   descriptor's <c> schemaVersion </c> (1 when absent).
/// </param>
/// <param name="MinAppVersion"> Minimum CLIHub version required; v1 entries only, null for v2. </param>
/// <param name="Publisher"> Plugin publisher. </param>
/// <param name="Homepage"> Plugin homepage URL. </param>
/// <param name="PackageUrl">
///   HTTPS URL to the plugin package; null for descriptor-only v2 entries.
/// </param>
/// <param name="Sha256"> Hex-encoded SHA-256 checksum of the package; null for descriptor-only v2 entries. </param>
/// <param name="ReleaseNotesUrl"> Optional release notes URL. </param>
/// <param name="Signature"> Reserved for future signing; never verified. </param>
/// <param name="Compatibility">
///   Compatibility range the current app version must satisfy: <c> "&gt;=" + MinAppVersion </c>
///   for v1 entries, the raw range text for v2 entries.
/// </param>
/// <param name="Channel"> Release channel; v2 entries only, null for v1. </param>
/// <param name="DescriptorJson"> Verbatim inline descriptor JSON; v2 entries only, null for v1. </param>
/// <param name="LogoUrl"> Optional logo image URL; v2 entries only, null for v1. </param>
public sealed record PluginFeedEntry(
    string Id,
    string Name,
    string Description,
    string Version,
    int SchemaVersion,
    string? MinAppVersion,
    string Publisher,
    string Homepage,
    string? PackageUrl,
    string? Sha256,
    string? ReleaseNotesUrl,
    string? Signature,
    string Compatibility,
    string? Channel,
    string? DescriptorJson,
    string? LogoUrl);

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

/// <summary>
///   Result of a logo download operation for a descriptor-only install.
/// </summary>
/// <param name="Success"> Whether the logo was successfully downloaded. </param>
/// <param name="LogoBytes">
///   Logo image bytes, or null on failure. Display-only; no checksum is verified — the feed
///   index is the integrity anchor per ADR-0015.
/// </param>
/// <param name="Errors"> Error messages, if any. </param>
public sealed record PluginLogoDownloadResult(
    bool Success,
    byte[]? LogoBytes,
    IReadOnlyList<string> Errors);
