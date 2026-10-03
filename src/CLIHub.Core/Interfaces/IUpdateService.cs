namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Checks for application updates and reports the current version.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    ///   Returns the current application version, or "unknown" when it cannot be determined.
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    ///   Checks for an available update. Never throws; failures and timeouts are reported
    ///   via the result status.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///   Gets a value indicating whether an update download is currently running.
    /// </summary>
    bool IsDownloading { get; }

    /// <summary>
    ///   Gets the version of the most recent update found by a check, or null when the last
    ///   check found nothing usable.
    /// </summary>
    string? LastKnownAvailableVersion { get; }

    /// <summary>
    ///   Raised on a background thread when the update state changes: a check finds (or stops
    ///   finding) an update, or a download starts or finishes.
    /// </summary>
    event EventHandler? UpdateStateChanged;

    /// <summary>
///   Checks for an update and downloads it when one is available. Never throws; failures
///   are reported via the result status. A second call while a download runs reports
///   <see cref="UpdateDownloadStatus.AlreadyDownloading"/> without starting another one.
    /// </summary>
    Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
///   Applies the downloaded update and restarts the application. Call only after
///   <see cref="DownloadUpdateAsync"/> reported a download; throws when applying fails.
    /// </summary>
    void ApplyDownloadedUpdateAndRestart();
}
