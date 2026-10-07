namespace CLIHub.Core.Updates;

using CLIHub.Core.Models;

/// <summary>
///   Checks whether an application update is available.
/// </summary>
public interface IUpdateChecker
{
    /// <summary>
    ///   Checks for an available update without downloading or applying it.
    /// </summary>
    /// <param name="cancellationToken"> Token that cancels the check. </param>
    /// <returns> The check outcome. </returns>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
