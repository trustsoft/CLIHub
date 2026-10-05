namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Adapts the shared configuration document to the preferences boundary.
/// </summary>
public sealed class PreferencesStore : IPreferencesStore
{
    private readonly IConfigService _configService;

    /// <summary>
    ///   Creates the store over the shared configuration service.
    /// </summary>
    /// <param name="configService"> The shared configuration service. </param>
    public PreferencesStore(IConfigService configService)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
    }

    /// <inheritdoc />
    public AppPreferences Load() => _configService.Load().Preferences;

    /// <inheritdoc />
    public void Save(AppPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var snapshot = _configService.Load();
        snapshot.Preferences = preferences;
        _configService.Save(snapshot);
    }
}
