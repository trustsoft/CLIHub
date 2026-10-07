namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Runs the optional startup update check and reports available versions.
/// </summary>
public sealed class UpdateStartupCoordinator : IUpdateStartupCoordinator
{
    private readonly IUpdateChecker _updateChecker;
    private readonly ILogger<UpdateStartupCoordinator> _logger;

    /// <summary>
    ///   Creates the coordinator over the update service.
    /// </summary>
    /// <param name="updateChecker"> Update checker used for the startup check. </param>
    /// <param name="logger"> Logger for disabled checks and unexpected failures. </param>
    public UpdateStartupCoordinator(
        IUpdateChecker updateChecker,
        ILogger<UpdateStartupCoordinator> logger)
    {
        _updateChecker = updateChecker ?? throw new ArgumentNullException(nameof(updateChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task CheckAsync(
        bool enabled,
        Action<string> notifyUpdateAvailable,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifyUpdateAvailable);

        if (!enabled)
        {
            _logger.LogInformation("Startup update check disabled in preferences");
            return;
        }

        try
        {
            var result = await _updateChecker.CheckForUpdatesAsync(cancellationToken);

            if (result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is { } version)
            {
                notifyUpdateAvailable(version);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Startup update check cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
        }
    }
}
