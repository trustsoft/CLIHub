namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Coordinates optional best-effort startup operations.
/// </summary>
public sealed class OptionalStartupCoordinator : IOptionalStartupCoordinator
{
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IReleaseNotesStartupCoordinator _releaseNotesStartup;
    private readonly IUpdateStartupCoordinator _updateStartup;
    private readonly ILogger<OptionalStartupCoordinator> _logger;

    /// <summary>
///   Creates the optional startup coordinator.
/// </summary>
/// <param name="operationLifetime"> Application operation lifetime boundary. </param>
    /// <param name="releaseNotesStartup"> Release-notes startup workflow. </param>
    /// <param name="updateStartup"> Startup update-check workflow. </param>
    /// <param name="logger"> Logger for optional operation failures. </param>
    public OptionalStartupCoordinator(
        IApplicationOperationLifetime operationLifetime,
        IReleaseNotesStartupCoordinator releaseNotesStartup,
        IUpdateStartupCoordinator updateStartup,
        ILogger<OptionalStartupCoordinator> logger)
    {
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _releaseNotesStartup = releaseNotesStartup ?? throw new ArgumentNullException(nameof(releaseNotesStartup));
        _updateStartup = updateStartup ?? throw new ArgumentNullException(nameof(updateStartup));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void RunOptionalStartup(
        StartupState startupState,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(startupState);
        ArgumentNullException.ThrowIfNull(startupUi);
        ArgumentNullException.ThrowIfNull(context);

        RunBestEffort("release notes", () => _releaseNotesStartup.Evaluate(startupState.Preferences));
        RunBestEffort(
            "startup update check",
            () => _ = _operationLifetime.RunAsync(
                "Startup update check",
                cancellationToken => RunStartupUpdateCheckAsync(
                    startupState.Preferences.CheckForUpdatesOnStartup,
                    startupUi,
                    context,
                    cancellationToken)));
    }

    private async Task RunStartupUpdateCheckAsync(
        bool enabled,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await _updateStartup.CheckAsync(
                enabled,
                version => context.Dispatch(() => startupUi.NotifyUpdateAvailable(version)),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup update check failed");
        }
    }

    private void RunBestEffort(string operation, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Optional startup operation failed: {Operation}", operation);
        }
    }
}
