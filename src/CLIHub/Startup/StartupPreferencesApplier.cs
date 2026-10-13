namespace CLIHub.Startup;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Infrastructure.Windows;
using CLIHub.Core.Models;

/// <summary>
///   Applies the startup-related portion of the application preferences.
/// </summary>
public sealed class StartupPreferencesApplier : IStartupPreferencesApplier
{
    private readonly ProcessLauncher processLauncher;
    private readonly IStartupService startupService;

    /// <summary>
    ///   Creates the applier with the process runtime and Windows startup boundaries.
    /// </summary>
    /// <param name="processLauncher"> Process launcher receiving the runtime selection. </param>
    /// <param name="startupService"> Windows startup registration service. </param>
    public StartupPreferencesApplier(
        ProcessLauncher processLauncher,
        IStartupService startupService)
    {
        this.processLauncher = processLauncher ?? throw new ArgumentNullException(nameof(processLauncher));
        this.startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
    }

    /// <inheritdoc />
    public void Apply(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        this.processLauncher.SetRuntime(preferences.DefaultRuntime);
        this.startupService.SetEnabled(preferences.StartWithWindows);
    }
}
