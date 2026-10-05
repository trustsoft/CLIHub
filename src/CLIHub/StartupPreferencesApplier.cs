namespace CLIHub;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

/// <summary>
///   Applies the startup-related portion of the application preferences.
/// </summary>
public sealed class StartupPreferencesApplier : IStartupPreferencesApplier
{
    private readonly IInteractiveProcessRunner _processRunner;
    private readonly IStartupService _startupService;

    /// <summary>
    ///   Creates the applier with the process runtime and Windows startup boundaries.
    /// </summary>
    /// <param name="processRunner"> Process runner receiving the runtime selection. </param>
    /// <param name="startupService"> Windows startup registration service. </param>
    public StartupPreferencesApplier(
        IInteractiveProcessRunner processRunner,
        IStartupService startupService)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
    }

    /// <inheritdoc />
    public void Apply(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        _processRunner.SetRuntime(RuntimeKinds.Parse(preferences.DefaultRuntime));
        _startupService.SetEnabled(preferences.StartWithWindows);
    }
}
