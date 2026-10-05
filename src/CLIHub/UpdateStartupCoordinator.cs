namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;
using CLIHub.Core.Updates;

/// <summary>
///   Runs the optional startup update check and reports available versions.
/// </summary>
public sealed class UpdateStartupCoordinator : IUpdateStartupCoordinator
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdateStartupCoordinator> _logger;

    /// <summary>
    ///   Creates the coordinator over the update service.
    /// </summary>
    /// <param name="updateService"> Update service used for the startup check. </param>
    /// <param name="logger"> Logger for disabled checks and unexpected failures. </param>
    public UpdateStartupCoordinator(
        IUpdateService updateService,
        ILogger<UpdateStartupCoordinator> logger)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task CheckAsync(bool enabled, Action<string> notifyUpdateAvailable)
    {
        ArgumentNullException.ThrowIfNull(notifyUpdateAvailable);

        if (!enabled)
        {
            _logger.LogInformation("Startup update check disabled in preferences");
            return;
        }

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();

            if (result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is { } version)
            {
                notifyUpdateAvailable(version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
        }
    }
}
