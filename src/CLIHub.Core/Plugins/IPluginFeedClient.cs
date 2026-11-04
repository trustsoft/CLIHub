namespace CLIHub.Core.Plugins;

/// <summary>
///   Fetches the plugin feed index and downloads plugin packages over HTTPS with checksum verification.
/// </summary>
public interface IPluginFeedClient
{
    /// <summary>
    ///   Fetches and parses the plugin feed index from the configured URL.
    /// </summary>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> Feed result with index or errors. </returns>
    Task<PluginFeedResult> GetFeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Downloads a plugin package to a temporary file and verifies its checksum.
    /// </summary>
    /// <param name="packageUrl"> HTTPS URL to the .clihub-plugin.zip file. </param>
    /// <param name="expectedSha256"> Hex-encoded SHA-256 checksum from the feed index. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <returns> Download result with temporary file path or errors. </returns>
    Task<PluginDownloadResult> DownloadPackageAsync(string packageUrl, string expectedSha256, CancellationToken cancellationToken = default);
}
