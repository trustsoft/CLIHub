namespace CLIHub.Core.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

using Velopack;

/// <summary>
///   Manages download state and orchestrates update downloads.
/// </summary>
internal sealed class UpdateDownloader
{
    private readonly ILogger _logger;
    private readonly object _downloadGate = new();
    private bool _isDownloading;
    private VelopackAsset? _downloadedAsset;

    /// <summary>
    ///   Creates the update downloader.
    /// </summary>
    /// <param name="logger"> Logger for download operations. </param>
    public UpdateDownloader(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Gets a value indicating whether a download is currently in progress.
    /// </summary>
    public bool IsDownloading
    {
        get
        {
            lock (_downloadGate)
            {
                return _isDownloading;
            }
        }
    }

    /// <summary>
    ///   Gets the most recently downloaded asset, or null if no download has completed.
    /// </summary>
    public VelopackAsset? DownloadedAsset
    {
        get
        {
            lock (_downloadGate)
            {
                return _downloadedAsset;
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

        lock (_downloadGate)
        {
            if (_isDownloading)
            {
                _logger.LogInformation("Update download requested while another download is running");
                return new UpdateDownloadResult(UpdateDownloadStatus.AlreadyDownloading, null);
            }

            _isDownloading = true;
        }

        try
        {
            var asset = update.TargetFullRelease;
            if (asset is null)
            {
                _logger.LogWarning("Update has no target release asset");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, null);
            }

            var version = Normalize(asset.Version.ToString());

            try
            {
                await manager.DownloadUpdatesAsync(update, null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update download cancelled");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, version);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Update download failed");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, version);
            }

            lock (_downloadGate)
            {
                _downloadedAsset = asset;
            }

            _logger.LogInformation("Update downloaded: {Version}", version);
            return new UpdateDownloadResult(UpdateDownloadStatus.Downloaded, version);
        }
        finally
        {
            lock (_downloadGate)
            {
                _isDownloading = false;
            }
        }
    }

    /// <summary>
    ///   Strips build metadata (for example, the "+commit" suffix) so the version
    ///   displays as a plain semantic version.
    /// </summary>
    private static string Normalize(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
