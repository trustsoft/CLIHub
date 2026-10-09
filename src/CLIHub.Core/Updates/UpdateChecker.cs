namespace CLIHub.Core.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

using Velopack;
using Velopack.Exceptions;

/// <summary>
///   Handles update checking with timeout and error handling.
/// </summary>
internal sealed class UpdateChecker
{
    private readonly ILogger _logger;

    /// <summary>
    ///   Creates the update checker.
    /// </summary>
    /// <param name="logger"> Logger for check operations. </param>
    public UpdateChecker(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Checks for an available update with timeout handling.
    /// </summary>
    /// <param name="manager"> The UpdateManager to use for checking. </param>
    /// <param name="currentVersion"> The current application version. </param>
    /// <param name="timeout"> Maximum time to wait for the check. </param>
    /// <param name="cancellationToken"> Token that cancels the check. </param>
    /// <returns> The check result and update info if available. </returns>
    public async Task<(UpdateCheckResult Result, UpdateInfo? Update)> CheckAsync(
        UpdateManager manager,
        string currentVersion,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentNullException.ThrowIfNull(currentVersion);

        try
        {
            var checkTask = manager.CheckForUpdatesAsync();
            var timeoutTask = Task.Delay(timeout, cancellationToken);
            var completed = await Task.WhenAny(checkTask, timeoutTask);

            if (completed != checkTask)
            {
                _ = ObserveLateUpdateCheckAsync(checkTask);
                _logger.LogWarning("Update check timed out after {Seconds}s", timeout.TotalSeconds);
                return (new UpdateCheckResult(UpdateStatus.Failed, currentVersion, null), null);
            }

            var update = await checkTask;
            if (update?.TargetFullRelease?.Version is { } version)
            {
                var available = Normalize(version.ToString());
                _logger.LogInformation("Update available: {Version}", available);
                return (new UpdateCheckResult(UpdateStatus.UpdateAvailable, currentVersion, available), update);
            }

            _logger.LogInformation("No update available");
            return (new UpdateCheckResult(UpdateStatus.UpToDate, currentVersion, null), null);
        }
        catch (NotInstalledException)
        {
            _logger.LogInformation("Update check skipped: the application is not a Velopack install");
            return (new UpdateCheckResult(UpdateStatus.NotInstalled, currentVersion, null), null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check cancelled");
            return (new UpdateCheckResult(UpdateStatus.Failed, currentVersion, null), null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return (new UpdateCheckResult(UpdateStatus.Failed, currentVersion, null), null);
        }
    }

    private async Task ObserveLateUpdateCheckAsync(Task<UpdateInfo?> checkTask)
    {
        try
        {
            await checkTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Late update check completed after timeout");
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
