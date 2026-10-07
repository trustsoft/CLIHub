namespace CLIHub;

/// <summary>
///   Coordinates the shared update download and apply/restart workflow.
/// </summary>
public interface IUpdateDownloadCoordinator
{
    /// <summary>
    ///   Downloads the known update, reports its outcome, and applies it with a restart when ready.
    /// </summary>
    /// <returns> A task that completes when the workflow has finished. </returns>
    Task DownloadAndApplyAsync(CancellationToken cancellationToken = default);
}
