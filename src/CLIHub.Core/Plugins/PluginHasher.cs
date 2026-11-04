namespace CLIHub.Core.Plugins;

using System.Security.Cryptography;
using System.Text;

/// <summary>
///   Computes the hex-encoded SHA-256 content hashes recorded in the plugin state file. The
///   hashes detect content drift and user modification relative to what CLIHub last wrote;
///   they are integrity markers, not signatures.
/// </summary>
public static class PluginHasher
{
    /// <summary>
    ///   Computes the hex-encoded SHA-256 hash of raw content.
    /// </summary>
    /// <param name="content"> The content to hash. </param>
    /// <returns> The lowercase hex-encoded hash. </returns>
    public static string ComputeHash(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
    }

    /// <summary>
    ///   Computes the hex-encoded SHA-256 hash of a text's UTF-8 bytes.
    /// </summary>
    /// <param name="content"> The text to hash. </param>
    /// <returns> The lowercase hex-encoded hash. </returns>
    public static string ComputeHash(string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return ComputeHash(Encoding.UTF8.GetBytes(content));
    }

    /// <summary>
    ///   Computes the hex-encoded SHA-256 hash of a file's raw bytes.
    /// </summary>
    /// <param name="path"> The file to hash. </param>
    /// <returns> The lowercase hex-encoded hash, or null when the file does not exist. </returns>
    public static string? ComputeFileHash(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return File.Exists(path)
            ? ComputeHash(File.ReadAllBytes(path))
            : null;
    }
}
