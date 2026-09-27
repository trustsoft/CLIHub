using CLIHub.Core.Models;

namespace CLIHub.Core.Interfaces;

/// <summary>
/// Checks for application updates and reports the current version.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Returns the current application version, or "unknown" when it cannot be determined.
    /// </summary>
    string GetCurrentVersion();

    /// <summary>
    /// Checks for an available update. Never throws; failures and timeouts are reported
    /// via the result status.
    /// </summary>
    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
