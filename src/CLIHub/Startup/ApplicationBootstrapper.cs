namespace CLIHub.Startup;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Scanning;
using CLIHub.Lifetime;

/// <summary>
///   Coordinates required and best-effort first-instance startup operations.
/// </summary>
public sealed class ApplicationBootstrapper : IApplicationBootstrapper
{
    private readonly IInstanceCoordinator instanceCoordinator;
    private readonly IStartupStateLoader startupStateLoader;
    private readonly IApplicationSession session;
    private readonly Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory;
    private readonly IOptionalStartupCoordinator optionalStartup;
    private readonly IProjectScanner projectScanner;
    private readonly ILogger<ApplicationBootstrapper> logger;

    /// <summary>
    ///   Creates the application startup orchestrator.
    /// </summary>
    /// <param name="instanceCoordinator"> Single-instance coordinator. </param>
    /// <param name="startupStateLoader"> Startup state loader. </param>
    /// <param name="session"> First-instance session and application event owner. </param>
    /// <param name="hotkeyStartupRegistrarFactory"> Lazy hotkey startup workflow factory. </param>
    /// <param name="optionalStartup"> Optional startup operations coordinator. </param>
    /// <param name="projectScanner"> Background repository scanner. </param>
    /// <param name="logger"> Logger for fatal and unexpected startup failures. </param>
    public ApplicationBootstrapper(
        IInstanceCoordinator instanceCoordinator,
        IStartupStateLoader startupStateLoader,
        IApplicationSession session,
        Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory,
        IOptionalStartupCoordinator optionalStartup,
        IProjectScanner projectScanner,
        ILogger<ApplicationBootstrapper> logger)
    {
        this.instanceCoordinator = instanceCoordinator ?? throw new ArgumentNullException(nameof(instanceCoordinator));
        this.startupStateLoader = startupStateLoader ?? throw new ArgumentNullException(nameof(startupStateLoader));
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.hotkeyStartupRegistrarFactory = hotkeyStartupRegistrarFactory ?? throw new ArgumentNullException(nameof(hotkeyStartupRegistrarFactory));
        this.optionalStartup = optionalStartup ?? throw new ArgumentNullException(nameof(optionalStartup));
        this.projectScanner = projectScanner ?? throw new ArgumentNullException(nameof(projectScanner));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public void Start(ApplicationStartupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Dispatch);
        ArgumentNullException.ThrowIfNull(context.SetMainWindow);

        var instanceStatus = this.instanceCoordinator.Coordinate();
        if (instanceStatus == InstanceStatus.SecondInstance)
        {
            return;
        }

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
