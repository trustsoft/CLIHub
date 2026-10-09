namespace CLIHub;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

/// <summary>
///   Applies the startup-related portion of the application preferences.
/// </summary>
public sealed class StartupPreferencesApplier : IStartupPreferencesApplier
{
    private readonly ProcessLauncher _processLauncher;
    private readonly IStartupService _startupService;

    /// <summary>
    ///   Creates the applier with the process runtime and Windows startup boundaries.
    /// </summary>
    /// <param name="processLauncher"> Process launcher receiving the runtime selection. </param>
    /// <param name="startupService"> Windows startup registration service. </param>
    public StartupPreferencesApplier(
        ProcessLauncher processLauncher,
        IStartupService startupService)
    {
        _processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
        _startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
    }

    /// <inheritdoc />
    public void Apply(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        _processLauncher.SetRuntime(preferences.DefaultRuntime);
        _startupService.SetEnabled(preferences.StartWithWindows);
    }
}
