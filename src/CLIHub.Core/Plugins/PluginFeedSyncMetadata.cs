namespace CLIHub.Core.Plugins;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;

using Microsoft.Extensions.Logging;

/// <summary>
///   Sync metadata for the last successful plugin feed fetch, persisted next to the cached index in
///   <c> %APPDATA%\CLIHub\cache\feed-sync.json </c>: the HTTP validators used for conditional
///   requests, the time the cache was written, and the hash of the compared index tuples that drives
///   change detection. Every member is nullable so a partial or older file still reads.
/// </summary>
public sealed record PluginFeedSyncMetadata
{
    /// <summary>
    ///   The <c> ETag </c> response header of the last fetch, used as <c> If-None-Match </c> on the
    ///   next conditional request; null when the server provided none.
    /// </summary>
    [JsonPropertyName("etag")]
    public string? ETag { get; init; }

    /// <summary>
    ///   The <c> Last-Modified </c> response header of the last fetch, used as
    ///   <c> If-Modified-Since </c> when no ETag is known; null when the server provided none.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public string? LastModified { get; init; }

    /// <summary>
    ///   When the cached index was written, shown to the user as the cache age; null for a partial
    ///   file.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    ///   SHA-256 hash of the sorted <c> { id, version, compatibility } </c> tuples of the last parsed
    ///   index; null before the first successful refresh.
    /// </summary>
    [JsonPropertyName("indexHash")]
    public string? IndexHash { get; init; }

    /// <summary>
    ///   Reads the sync metadata tolerantly: a missing file yields null, a corrupt file is deleted
    ///   with a logged warning (sync metadata is disposable state and never crashes the caller), and
    ///   an I/O failure is logged and treated as absent without deleting the file.
    /// </summary>
    /// <param name="path"> The metadata file path. </param>
    /// <param name="logger"> Logger receiving the tolerance warnings. </param>
    /// <returns> The stored metadata, or null when absent, unreadable, or corrupt. </returns>
    internal static PluginFeedSyncMetadata? Load(string path, ILogger logger)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PluginFeedSyncMetadata>(json, CoreJson.Options);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Corrupt plugin feed sync metadata at {Path}; deleting it", path);
            TryDelete(path, logger);
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            logger.LogWarning(ex, "Could not read the plugin feed sync metadata at {Path}; treating it as absent", path);
            return null;
        }
    }

    /// <summary>
    ///   Writes the metadata atomically (temp file plus rename), so an interrupted write cannot
    ///   corrupt the previous validators. Write failures are the caller's to handle.
    /// </summary>
    /// <param name="path"> The metadata file path. </param>
    internal void Save(string path) =>
        AtomicFileWriter.Write(path, JsonSerializer.Serialize(this, CoreJson.Options));

    /// <summary>
    ///   Computes the change-detection hash of a parsed index: SHA-256 over the sorted
    ///   <c> { id, version, compatibility } </c> tuples. Order, descriptions, and logo URLs do not
    ///   contribute, so a reorder or a cosmetic edit keeps the hash stable.
    /// </summary>
    /// <param name="plugins"> The parsed index entries. </param>
    /// <returns> The hex-encoded SHA-256 hash. </returns>
    internal static string ComputeIndexHash(IReadOnlyList<PluginFeedEntry> plugins)
    {
        ArgumentNullException.ThrowIfNull(plugins);

        var builder = new StringBuilder();
        foreach (var entry in plugins.OrderBy(entry => entry.Id, StringComparer.Ordinal))
        {
            builder.Append(entry.Id)
                .Append('\n')
                .Append(entry.Version)
                .Append('\n')
                .Append(entry.Compatibility)
                .Append('\n');
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString())));
    }

    /// <summary>
    ///   Deletes a disposable cache file, logging instead of throwing so a locked or already removed
    ///   file never breaks a refresh.
    /// </summary>
    /// <param name="path"> The file path to delete. </param>
    /// <param name="logger"> Logger receiving deletion failures. </param>
    internal static void TryDelete(string path, ILogger logger)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            logger.LogWarning(ex, "Could not delete the plugin feed cache file {Path}", path);
        }
    }
}
