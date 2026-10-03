namespace CLIHub.Core.Models;

/// <summary>
///   Outcome of an update download request.
/// </summary>
public enum UpdateDownloadStatus
{
    /// <summary>
    ///   The update was downloaded and is ready to apply.
    /// </summary>
    Downloaded,

    /// <summary>
    ///   The download was requested while another download was already running.
    /// </summary>
    AlreadyDownloading,

    /// <summary>
    ///   No update is available to download.
    /// </summary>
    NoUpdate,

    /// <summary>
    ///   The application is not an updater-managed install, so updates cannot be downloaded.
    /// </summary>
    NotInstalled,

    /// <summary>
    ///   The check or download failed.
    /// </summary>
    Failed
}

/// <summary>
///   Result of an update download request.
/// </summary>
/// <param name="Status"> The download outcome. </param>
/// <param name="AvailableVersion"> The version the outcome refers to: the downloaded version when <see cref="UpdateDownloadStatus.Downloaded"/>, the version whose download failed when <see cref="UpdateDownloadStatus.Failed"/> after a successful check, otherwise null. </param>
public sealed record UpdateDownloadResult(UpdateDownloadStatus Status, string? AvailableVersion);
