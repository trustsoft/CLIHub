namespace CLIHub.Startup;

using CLIHub.Core.Configuration;

/// <summary>
///   Loads required startup state before the application session begins.
/// </summary>
public sealed class StartupStateLoader : IStartupStateLoader
{
    private readonly IPluginInitializationService pluginInitialization;
    private readonly IPreferencesStore preferencesStore;
    private readonly IStartupPreferencesApplier startupPreferences;

    /// <summary>
    ///   Creates the startup state loader.
    /// </summary>
    /// <param name="pluginInitialization"> Plugin initialization workflow. </param>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupPreferences"> Startup preference applier. </param>
    public StartupStateLoader(IPluginInitializationService pluginInitialization,
                              IPreferencesStore preferencesStore,
                              IStartupPreferencesApplier startupPreferences)
    {
        this.pluginInitialization = pluginInitialization ?? throw new ArgumentNullException(nameof(pluginInitialization));
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.startupPreferences = startupPreferences ?? throw new ArgumentNullException(nameof(startupPreferences));
    }

    /// <inheritdoc />
    public StartupState Load()
    {
        this.pluginInitialization.Initialize();

        var preferences = this.preferencesStore.Load();
        this.startupPreferences.Apply(preferences);

        return new StartupState(preferences);
    }
}
