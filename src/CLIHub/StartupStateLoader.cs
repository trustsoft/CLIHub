namespace CLIHub;

using CLIHub.Core.Configuration;

/// <summary>
///   Loads required startup state before the application session begins.
/// </summary>
public sealed class StartupStateLoader : IStartupStateLoader
{
    private readonly IPluginInitializationService _pluginInitialization;
    private readonly IPreferencesStore _preferencesStore;
    private readonly IStartupPreferencesApplier _startupPreferences;

    /// <summary>
    ///   Creates the startup state loader.
    /// </summary>
    /// <param name="pluginInitialization"> Plugin initialization workflow. </param>
    /// <param name="preferencesStore"> Preferences persistence boundary. </param>
    /// <param name="startupPreferences"> Startup preference applier. </param>
    public StartupStateLoader(
        IPluginInitializationService pluginInitialization,
        IPreferencesStore preferencesStore,
        IStartupPreferencesApplier startupPreferences)
    {
        _pluginInitialization = pluginInitialization ?? throw new ArgumentNullException(nameof(pluginInitialization));
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _startupPreferences = startupPreferences ?? throw new ArgumentNullException(nameof(startupPreferences));
    }

    /// <inheritdoc />
    public StartupState Load()
    {
        _pluginInitialization.Initialize();

        var preferences = _preferencesStore.Load();
        _startupPreferences.Apply(preferences);

        return new StartupState(preferences);
    }
}
