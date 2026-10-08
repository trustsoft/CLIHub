namespace CLIHub;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates update checks, downloads, state, and entry-point apply policies.
/// </summary>
public interface IUpdateWorkflow :
    IUpdateVersionProvider,
    IUpdateChecker,
    IUpdateStateSource,
    IUpdateDownloader,
    IUpdateInstaller
{
    /// <summary>
    ///   Gets a value indicating whether a shared update check is currently running.
    /// </summary>
    bool IsCheckingForUpdates { get; }

    /// <summary>
    ///   Raised after an automatic download flow succeeds and is about to restart.
    /// </summary>
    event EventHandler<string>? UpdateDownloaded;

    /// <summary>
    ///   Raised when an automatic download flow fails for a known version.
    /// </summary>
    event EventHandler<string>? UpdateDownloadFailed;

    /// <summary>
    ///   Downloads the known update and applies it with a restart.
    /// </summary>
    /// <param name="cancellationToken"> Token that cancels the tracked workflow. </param>
    /// <returns> A task that completes when the download/apply workflow finishes. </returns>
    Task DownloadAndApplyAsync(CancellationToken cancellationToken = default);
}
