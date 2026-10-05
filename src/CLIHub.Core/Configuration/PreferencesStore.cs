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
    public void Update(Action<AppPreferences> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        var snapshot = _configService.Load();
        update(snapshot.Preferences);
        _configService.Save(snapshot);
    }
}
