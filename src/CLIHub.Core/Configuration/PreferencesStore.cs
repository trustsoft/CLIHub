namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Adapts the configuration repository to the preferences' boundary.
/// </summary>
public sealed class PreferencesStore : IPreferencesStore
{
    private readonly IConfigurationRepository repository;

    /// <summary>
    ///   Creates the store over the configuration repository.
    /// </summary>
    /// <param name="repository"> The configuration repository. </param>
    public PreferencesStore(IConfigurationRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    /// <inheritdoc />
    public AppPreferences Load() => this.repository.Read().Preferences;

    /// <inheritdoc />
    public void Update(Action<AppPreferences> update)
    {
        ArgumentNullException.ThrowIfNull(update);

        this.repository.Update(snapshot => update(snapshot.Preferences));
    }
}
