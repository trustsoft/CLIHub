namespace CLIHub.Core.Updates;

using CLIHub.Core.Models;

/// <summary>
///   Downloads an available application update.
/// </summary>
public interface IUpdateDownloader
{
    /// <summary>
    ///   Checks for and downloads an available update.
    /// </summary>
    /// <param name="cancellationToken"> Token that cancels the download. </param>
    /// <returns> The download outcome. </returns>
    Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default);
}
