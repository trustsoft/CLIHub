namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Runs the shared update download, notification, and apply/restart workflow.
/// </summary>
public sealed class UpdateDownloadCoordinator : IUpdateDownloadCoordinator
{
    private static readonly TimeSpan NotificationDelay = TimeSpan.FromSeconds(2);

    private readonly IUpdateDownloader _downloader;
    private readonly IUpdateInstaller _installer;
    private readonly IUpdateDownloadNotifier _notifier;
    private readonly ILogger<UpdateDownloadCoordinator> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    /// <summary>
    ///   Creates the coordinator with the update service and UI notification boundary.
    /// </summary>
    /// <param name="downloader"> Update downloader used by the workflow. </param>
    /// <param name="installer"> Update installer used after a successful download. </param>
    /// <param name="notifier"> UI boundary for notifications and tray refreshes. </param>
    /// <param name="logger"> Logger for workflow failures and non-applied results. </param>
    /// <param name="delay"> Optional cancellation-aware delay implementation. </param>
    public UpdateDownloadCoordinator(
        IUpdateDownloader downloader,
        IUpdateInstaller installer,
        IUpdateDownloadNotifier notifier,
        ILogger<UpdateDownloadCoordinator> logger,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _downloader = downloader ?? throw new ArgumentNullException(nameof(downloader));
        _installer = installer ?? throw new ArgumentNullException(nameof(installer));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delay = delay ?? ((duration, cancellationToken) => Task.Delay(duration, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DownloadAndApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _downloader.DownloadUpdateAsync(cancellationToken);

            if (result.Status == UpdateDownloadStatus.Downloaded && result.AvailableVersion is { } version)
            {
                _notifier.NotifyDownloaded(version);
                _notifier.RefreshMenu();
                await _delay(NotificationDelay, cancellationToken);
                _installer.ApplyDownloadedUpdateAndRestart();
            }
            else if (result.Status == UpdateDownloadStatus.Failed)
            {
                _logger.LogWarning("Update download failed (version {Version})", result.AvailableVersion);
                if (result.AvailableVersion is { } failedVersion)
                {
                    _notifier.NotifyFailed(failedVersion);
                }

                _notifier.RefreshMenu();
            }
            else
            {
                _logger.LogInformation("Update download not applied: {Status}", result.Status);
                _notifier.RefreshMenu();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Update download cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Applying the downloaded update failed");
            _notifier.RefreshMenu();
        }
    }
}
