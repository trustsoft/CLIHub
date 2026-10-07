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

    private readonly IUpdateService _updateService;
    private readonly IUpdateDownloadNotifier _notifier;
    private readonly ILogger<UpdateDownloadCoordinator> _logger;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    /// <summary>
    ///   Creates the coordinator with the update service and UI notification boundary.
    /// </summary>
    /// <param name="updateService"> Update service used for download and apply operations. </param>
    /// <param name="notifier"> UI boundary for notifications and tray refreshes. </param>
    /// <param name="logger"> Logger for workflow failures and non-applied results. </param>
    /// <param name="delay"> Optional cancellation-aware delay implementation. </param>
    public UpdateDownloadCoordinator(
        IUpdateService updateService,
        IUpdateDownloadNotifier notifier,
        ILogger<UpdateDownloadCoordinator> logger,
        Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _delay = delay ?? ((duration, cancellationToken) => Task.Delay(duration, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DownloadAndApplyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _updateService.DownloadUpdateAsync(cancellationToken);

            if (result.Status == UpdateDownloadStatus.Downloaded && result.AvailableVersion is { } version)
            {
                _notifier.NotifyDownloaded(version);
                _notifier.RefreshMenu();
                await _delay(NotificationDelay, cancellationToken);
                _updateService.ApplyDownloadedUpdateAndRestart();
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
