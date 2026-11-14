namespace CLIHub.Startup;

using CLIHub.Core.Plugins;
using CLIHub.Core.Scanning;
using CLIHub.Lifetime;

using Microsoft.Extensions.Logging;

/// <summary>
///   Coordinates required and best-effort first-instance startup operations.
/// </summary>
public sealed class ApplicationBootstrapper : IApplicationBootstrapper
{
    private readonly IStartupStateLoader startupStateLoader;
    private readonly IApplicationSession session;
    private readonly Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory;
    private readonly IOptionalStartupCoordinator optionalStartup;
    private readonly IProjectScanner projectScanner;
    private readonly IPluginFeedSyncService feedSyncService;
    private readonly ILogger<ApplicationBootstrapper> logger;

    /// <summary>
    ///   Creates the application startup orchestrator.
    /// </summary>
    /// <param name="startupStateLoader"> Startup state loader. </param>
    /// <param name="session"> First-instance session and application event owner. </param>
    /// <param name="hotkeyStartupRegistrarFactory"> Lazy hotkey startup workflow factory. </param>
    /// <param name="optionalStartup"> Optional startup operations coordinator. </param>
    /// <param name="projectScanner"> Background repository scanner. </param>
    /// <param name="feedSyncService"> Background plugin feed refresh service. </param>
    /// <param name="logger"> Logger for fatal and unexpected startup failures. </param>
    public ApplicationBootstrapper(IStartupStateLoader startupStateLoader,
                                   IApplicationSession session,
                                   Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory,
                                   IOptionalStartupCoordinator optionalStartup,
                                   IProjectScanner projectScanner,
                                   IPluginFeedSyncService feedSyncService,
                                   ILogger<ApplicationBootstrapper> logger)
    {
        this.startupStateLoader = startupStateLoader ?? throw new ArgumentNullException(nameof(startupStateLoader));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.hotkeyStartupRegistrarFactory = hotkeyStartupRegistrarFactory ?? throw new ArgumentNullException(nameof(hotkeyStartupRegistrarFactory));
        this.optionalStartup = optionalStartup ?? throw new ArgumentNullException(nameof(optionalStartup));
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.feedSyncService = feedSyncService ?? throw new ArgumentNullException(nameof(feedSyncService));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);
        ArgumentNullException.ThrowIfNull(context.SetMainWindow);

        try
        {
            var startupState = this.startupStateLoader.Load();

            var startupUi = this.session.Start(context);
            var hotkeyStartupRegistrar = this.hotkeyStartupRegistrarFactory();

            context.SetMainWindow(startupUi.LaunchWindow);

            if (!startupState.Preferences.StartMinimized)
            {
                startupUi.ShowLaunchWindow();
            }
            else
            {
                this.logger.LogInformation("Starting in the system tray (start minimized enabled)");
            }

            hotkeyStartupRegistrar.Register(startupState.Preferences);

            this.optionalStartup.RunOptionalStartup(startupState, startupUi, context);

            // Startup order: plugin initialization already ran inside the state loader, the UI is
            // live, and the optional network work is dispatched. The feed sync then starts here,
            // before the project scanner, joining the best-effort background services in one place.
            // Start() returns immediately; the first refresh runs on a background task, so startup
            // never waits for the network.
            this.feedSyncService.Start();

            this.projectScanner.Start();

            this.logger.LogInformation("CLIHub started");
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "CLIHub startup failed");
            throw;
        }
    }
}
