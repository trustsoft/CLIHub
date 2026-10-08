namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Coordinates required and best-effort first-instance startup operations.
/// </summary>
public sealed class ApplicationBootstrapper : IApplicationBootstrapper
{
    private readonly IInstanceCoordinator _instanceCoordinator;
    private readonly IApplicationOperationLifetime _operationLifetime;
    private readonly IPluginInitializationService _pluginInitialization;
    private readonly IPreferencesStore _preferencesStore;
    private readonly IStartupPreferencesApplier _startupPreferences;
    private readonly Func<IHotkeyStartupRegistrar> _hotkeyStartupRegistrarFactory;
    private readonly IReleaseNotesStartupCoordinator _releaseNotesStartup;
    private readonly IUpdateStartupCoordinator _updateStartup;
    private readonly IApplicationSession _session;
    private readonly ILogger<ApplicationBootstrapper> _logger;

    /// <summary>
    ///   Creates the application startup orchestrator.
    /// </summary>
    /// <param name="instanceCoordinator"> Single-instance coordinator. </param>
    /// <param name="operationLifetime"> Application operation lifetime boundary. </param>
    /// <param name="pluginInitialization"> Plugin initialization workflow. </param>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupPreferences"> Startup preference applier. </param>
    /// <param name="hotkeyStartupRegistrarFactory"> Lazy hotkey startup workflow factory. </param>
    /// <param name="releaseNotesStartup"> Release-notes startup workflow. </param>
    /// <param name="updateStartup"> Startup update-check workflow. </param>
    /// <param name="session"> First-instance session and application event owner. </param>
    /// <param name="logger"> Logger for fatal and unexpected startup failures. </param>
    public ApplicationBootstrapper(
        IInstanceCoordinator instanceCoordinator,
        IApplicationOperationLifetime operationLifetime,
        IPluginInitializationService pluginInitialization,
        IPreferencesStore preferencesStore,
        IStartupPreferencesApplier startupPreferences,
        Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory,
        IReleaseNotesStartupCoordinator releaseNotesStartup,
        IUpdateStartupCoordinator updateStartup,
        IApplicationSession session,
        ILogger<ApplicationBootstrapper> logger)
    {
        _instanceCoordinator = instanceCoordinator ?? throw new ArgumentNullException(nameof(instanceCoordinator));
        _operationLifetime = operationLifetime ?? throw new ArgumentNullException(nameof(operationLifetime));
        _pluginInitialization = pluginInitialization ?? throw new ArgumentNullException(nameof(pluginInitialization));
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _startupPreferences = startupPreferences ?? throw new ArgumentNullException(nameof(startupPreferences));
        _hotkeyStartupRegistrarFactory = hotkeyStartupRegistrarFactory ?? throw new ArgumentNullException(nameof(hotkeyStartupRegistrarFactory));
        _releaseNotesStartup = releaseNotesStartup ?? throw new ArgumentNullException(nameof(releaseNotesStartup));
        _updateStartup = updateStartup ?? throw new ArgumentNullException(nameof(updateStartup));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);
        ArgumentNullException.ThrowIfNull(context.SetMainWindow);

        var instanceStatus = _instanceCoordinator.Coordinate();
        if (instanceStatus == InstanceStatus.SecondInstance)
        {
            return;
        }

        try
        {
            _pluginInitialization.Initialize();

            var preferences = _preferencesStore.Load();
            _startupPreferences.Apply(preferences);

            var startupUi = _session.Start(context);
            var hotkeyStartupRegistrar = _hotkeyStartupRegistrarFactory();

            context.SetMainWindow(startupUi.LaunchWindow);

            if (preferences.ShowWindowOnStartup)
            {
                startupUi.ShowLaunchWindow();
            }
            else
            {
                _logger.LogInformation("Starting in the system tray (show window on startup disabled)");
            }

            hotkeyStartupRegistrar.Register(preferences);

            RunBestEffort("release notes", () => _releaseNotesStartup.Evaluate(preferences));
            RunBestEffort(
                "startup update check",
                () => _ = _operationLifetime.RunAsync(
                    "Startup update check",
                    cancellationToken => RunStartupUpdateCheckAsync(
                        preferences.CheckForUpdatesOnStartup,
                        startupUi,
                        context,
                        cancellationToken)));

            _logger.LogInformation("CLIHub started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CLIHub startup failed");
            throw;
        }
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
