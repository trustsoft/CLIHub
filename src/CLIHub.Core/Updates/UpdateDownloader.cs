namespace CLIHub.Core.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

using Velopack;

/// <summary>
///   Manages download state and orchestrates update downloads.
/// </summary>
internal sealed class UpdateDownloader
{
    private readonly ILogger logger;
    private readonly object downloadGate = new();
    private bool isDownloading;
    private VelopackAsset? downloadedAsset;

    /// <summary>
    ///   Creates the update downloader.
    /// </summary>
    /// <param name="logger"> Logger for download operations. </param>
    public UpdateDownloader(ILogger logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Gets the most recently downloaded asset, or null if no download has completed.
    /// </summary>
    public VelopackAsset? DownloadedAsset
    {
        get
        {
            lock (this.downloadGate)
            {
                return this.downloadedAsset;
            }
        }
    }

    /// <summary>
    ///   Downloads an available update.
    /// </summary>
    /// <param name="manager"> The UpdateManager to use for downloading. </param>
    /// <param name="update"> The update information. </param>
    /// <param name="cancellationToken"> Token that cancels the download. </param>
    /// <returns> The download result. </returns>
    public async Task<UpdateDownloadResult> DownloadAsync(
        UpdateManager manager,
        UpdateInfo update,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(update);

        lock (this.downloadGate)
        {
            if (this.isDownloading)
            {
                this.logger.LogInformation("Update download requested while another download is running");
                return new UpdateDownloadResult(UpdateDownloadStatus.AlreadyDownloading, null);
            }

            this.isDownloading = true;
        }

        try
        {
            var asset = update.TargetFullRelease;
            if (asset is null)
            {
                this.logger.LogWarning("Update has no target release asset");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, null);
            }

            var version = Normalize(asset.Version.ToString());

            try
            {
                await manager.DownloadUpdatesAsync(update, null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                this.logger.LogWarning("Update download cancelled");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, version);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Update download failed");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, version);
            }

            lock (this.downloadGate)
            {
                this.downloadedAsset = asset;
            }

            this.logger.LogInformation("Update downloaded: {Version}", version);
            return new UpdateDownloadResult(UpdateDownloadStatus.Downloaded, version);
        }
        finally
        {
            lock (this.downloadGate)
            {
                this.isDownloading = false;
            }
        }
    }

    /// <summary>
    ///   Strips build metadata (for example, the "+commit" suffix)
    ///   so the version displays as a plain semantic version.
    /// </summary>
    private static string Normalize(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
