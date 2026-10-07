namespace CLIHub;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Updates;

/// <summary>
///   Coordinates required and best-effort first-instance startup operations.
/// </summary>
public sealed class ApplicationBootstrapper : IApplicationBootstrapper
{
    private readonly ISingleInstanceGuard _singleInstanceGuard;
    private readonly IApplicationLifetime _applicationLifetime;
    private readonly IPluginInitializationService _pluginInitialization;
    private readonly IPreferencesStore _preferencesStore;
    private readonly IStartupPreferencesApplier _startupPreferences;
    private readonly Func<IHotkeyStartupRegistrar> _hotkeyStartupRegistrarFactory;
    private readonly IReleaseNotesStartupCoordinator _releaseNotesStartup;
    private readonly IUpdateStartupCoordinator _updateStartup;
    private readonly IUpdateService _updateService;
    private readonly Func<IApplicationStartupUi> _startupUiFactory;
    private readonly Func<IUpdateDownloadCoordinator> _updateDownloadFactory;
    private readonly Func<IUpdateRequestSource> _updateRequestSourceFactory;
    private readonly ILogger<ApplicationBootstrapper> _logger;

    /// <summary>
    ///   Creates the application startup orchestrator.
    /// </summary>
    /// <param name="singleInstanceGuard"> Single-instance boundary. </param>
    /// <param name="applicationLifetime"> Application shutdown boundary. </param>
    /// <param name="pluginInitialization"> Plugin initialization workflow. </param>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupPreferences"> Startup preference applier. </param>
    /// <param name="hotkeyStartupRegistrarFactory"> Lazy hotkey startup workflow factory. </param>
    /// <param name="releaseNotesStartup"> Release-notes startup workflow. </param>
    /// <param name="updateStartup"> Startup update-check workflow. </param>
    /// <param name="updateService"> Update state notification source. </param>
    /// <param name="startupUiFactory"> Lazy startup UI factory. </param>
    /// <param name="updateDownloadFactory"> Lazy update download workflow factory. </param>
    /// <param name="updateRequestSourceFactory"> Lazy presentation update-request source factory. </param>
    /// <param name="logger"> Logger for fatal and unexpected startup failures. </param>
    public ApplicationBootstrapper(
        ISingleInstanceGuard singleInstanceGuard,
        IApplicationLifetime applicationLifetime,
        IPluginInitializationService pluginInitialization,
        IPreferencesStore preferencesStore,
        IStartupPreferencesApplier startupPreferences,
        Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory,
        IReleaseNotesStartupCoordinator releaseNotesStartup,
        IUpdateStartupCoordinator updateStartup,
        IUpdateService updateService,
        Func<IApplicationStartupUi> startupUiFactory,
        Func<IUpdateDownloadCoordinator> updateDownloadFactory,
        Func<IUpdateRequestSource> updateRequestSourceFactory,
        ILogger<ApplicationBootstrapper> logger)
    {
        _singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _pluginInitialization = pluginInitialization ?? throw new ArgumentNullException(nameof(pluginInitialization));
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _startupPreferences = startupPreferences ?? throw new ArgumentNullException(nameof(startupPreferences));
        _hotkeyStartupRegistrarFactory = hotkeyStartupRegistrarFactory ?? throw new ArgumentNullException(nameof(hotkeyStartupRegistrarFactory));
        _releaseNotesStartup = releaseNotesStartup ?? throw new ArgumentNullException(nameof(releaseNotesStartup));
        _updateStartup = updateStartup ?? throw new ArgumentNullException(nameof(updateStartup));
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _startupUiFactory = startupUiFactory ?? throw new ArgumentNullException(nameof(startupUiFactory));
        _updateDownloadFactory = updateDownloadFactory ?? throw new ArgumentNullException(nameof(updateDownloadFactory));
        _updateRequestSourceFactory = updateRequestSourceFactory ?? throw new ArgumentNullException(nameof(updateRequestSourceFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);
        ArgumentNullException.ThrowIfNull(context.SetMainWindow);

        if (!_singleInstanceGuard.IsFirstInstance)
        {
            _singleInstanceGuard.SignalActivation();
            _applicationLifetime.Shutdown();
            return;
        }

        try
        {
            _pluginInitialization.Initialize();

            var preferences = _preferencesStore.Load();
            _startupPreferences.Apply(preferences);

            var startupUi = _startupUiFactory();
            var updateDownload = _updateDownloadFactory();
            var updateRequestSource = _updateRequestSourceFactory();
            var hotkeyStartupRegistrar = _hotkeyStartupRegistrarFactory();

            _updateService.UpdateStateChanged += (_, _) => context.Dispatch(startupUi.RefreshMenu);
            startupUi.UpdateDownloadRequested += (_, _) => _ = updateDownload.DownloadAndApplyAsync();
            updateRequestSource.UpdateRequested += (_, _) => _ = updateDownload.DownloadAndApplyAsync();
            _singleInstanceGuard.ActivationRequested += () => context.Dispatch(startupUi.ShowLaunchWindow);

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
                () => _ = RunStartupUpdateCheckAsync(preferences.CheckForUpdatesOnStartup, startupUi, context));

            _logger.LogInformation("CLIHub started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CLIHub startup failed");
            _applicationLifetime.Shutdown();
        }
    }

    private async Task RunStartupUpdateCheckAsync(
        bool enabled,
        IApplicationStartupUi startupUi,
        ApplicationStartupContext context)
    {
        try
        {
            await _updateStartup.CheckAsync(
                enabled,
                version => context.Dispatch(() => startupUi.NotifyUpdateAvailable(version)));
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
