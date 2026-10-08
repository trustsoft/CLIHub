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
    private readonly IStartupStateLoader _startupStateLoader;
    private readonly IApplicationSession _session;
    private readonly Func<IHotkeyStartupRegistrar> _hotkeyStartupRegistrarFactory;
    private readonly IOptionalStartupCoordinator _optionalStartup;
    private readonly ILogger<ApplicationBootstrapper> _logger;

    /// <summary>
    ///   Creates the application startup orchestrator.
    /// </summary>
    /// <param name="instanceCoordinator"> Single-instance coordinator. </param>
    /// <param name="startupStateLoader"> Startup state loader. </param>
    /// <param name="session"> First-instance session and application event owner. </param>
    /// <param name="hotkeyStartupRegistrarFactory"> Lazy hotkey startup workflow factory. </param>
    /// <param name="optionalStartup"> Optional startup operations coordinator. </param>
    /// <param name="logger"> Logger for fatal and unexpected startup failures. </param>
    public ApplicationBootstrapper(
        IInstanceCoordinator instanceCoordinator,
        IStartupStateLoader startupStateLoader,
        IApplicationSession session,
        Func<IHotkeyStartupRegistrar> hotkeyStartupRegistrarFactory,
        IOptionalStartupCoordinator optionalStartup,
        ILogger<ApplicationBootstrapper> logger)
    {
        _instanceCoordinator = instanceCoordinator ?? throw new ArgumentNullException(nameof(instanceCoordinator));
        _startupStateLoader = startupStateLoader ?? throw new ArgumentNullException(nameof(startupStateLoader));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _hotkeyStartupRegistrarFactory = hotkeyStartupRegistrarFactory ?? throw new ArgumentNullException(nameof(hotkeyStartupRegistrarFactory));
        _optionalStartup = optionalStartup ?? throw new ArgumentNullException(nameof(optionalStartup));
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
            var startupState = _startupStateLoader.Load();

            var startupUi = _session.Start(context);
            var hotkeyStartupRegistrar = _hotkeyStartupRegistrarFactory();

            context.SetMainWindow(startupUi.LaunchWindow);

            if (startupState.Preferences.ShowWindowOnStartup)
            {
                startupUi.ShowLaunchWindow();
            }
            else
            {
                _logger.LogInformation("Starting in the system tray (show window on startup disabled)");
            }

            hotkeyStartupRegistrar.Register(startupState.Preferences);

            _optionalStartup.RunOptionalStartup(startupState, startupUi, context);

            _logger.LogInformation("CLIHub started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CLIHub startup failed");
            throw;
        }
    }
}
