namespace CLIHub.Startup;

using CLIHub.Core.Configuration;
using CLIHub.Core.Windows;
using CLIHub.Core.Models;

/// <summary>
///   Applies the startup-related portion of the application preferences.
/// </summary>
public sealed class StartupPreferencesApplier : IStartupPreferencesApplier
{
    private readonly IPreferencesStore preferencesStore;
    private readonly IStartupService startupService;

    /// <summary>
    ///   Creates the applier with the preferences store and Windows startup boundaries.
    /// </summary>
    /// <param name="preferencesStore"> Preferences store for persisting runtime. </param>
    /// <param name="startupService"> Windows startup registration service. </param>
    public StartupPreferencesApplier(
        IPreferencesStore preferencesStore,
        IStartupService startupService)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.startupService = startupService ?? throw new ArgumentNullException(nameof(startupService));
    }

    /// <inheritdoc />
    public void Apply(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        // Runtime preference is already in the preferences store; no need to set it
        // ProcessOrchestrator reads it directly at launch time
        this.startupService.SetEnabled(preferences.StartWithWindows);
    }
}
