namespace CLIHub.Startup;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Updates;
using CLIHub.Updates;

/// <summary>
///   Runs the optional startup update check and reports available versions.
/// </summary>
public sealed class UpdateStartupCoordinator : IUpdateStartupCoordinator
{
    private readonly IUpdateWorkflow updateWorkflow;
    private readonly IPreferencesStore preferences;
    private readonly ILogger<UpdateStartupCoordinator> logger;

    /// <summary>
    ///   Creates the coordinator over the update service.
    /// </summary>
    /// <param name="updateWorkflow"> Shared application update workflow. </param>
    /// <param name="preferencesStore"> Preferences source for notification gating. </param>
    /// <param name="logger"> Logger for disabled checks and unexpected failures. </param>
    public UpdateStartupCoordinator(IUpdateWorkflow updateWorkflow,
                                    IPreferencesStore preferencesStore,
                                    ILogger<UpdateStartupCoordinator> logger)
    {
        this.updateWorkflow = updateWorkflow ?? throw new ArgumentNullException(nameof(updateWorkflow));
        this.preferences = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task CheckAsync(bool enabled,
                                 Action<string> notifyUpdateAvailable,
                                 CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notifyUpdateAvailable);

        if (!enabled)
        {
            this.logger.LogInformation("Startup update check disabled in preferences");
            return;
        }

        try
        {
            var result = await this.updateWorkflow.CheckForUpdatesAsync(cancellationToken);

            if (result.Status == UpdateStatus.UpdateAvailable && result.AvailableVersion is { } version)
            {
                var appPrefs = this.preferences.Load();
                var shouldNotify = VersionClassification.IsPrerelease(version)
                    ? appPrefs.NotifyPrereleaseUpdates
                    : appPrefs.NotifyStableUpdates;

                if (shouldNotify)
                {
                    notifyUpdateAvailable(version);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            this.logger.LogInformation("Startup update check cancelled");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Update check failed");
        }
    }
}
