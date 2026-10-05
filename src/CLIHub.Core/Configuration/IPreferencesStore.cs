namespace CLIHub.Core.Configuration;

using CLIHub.Core.Models;

/// <summary>
///   Provides access to application preferences without exposing the full configuration document.
/// </summary>
public interface IPreferencesStore
{
    /// <summary>
    ///   Loads the current application preferences.
    /// </summary>
    /// <returns> The application preferences. </returns>
    AppPreferences Load();

    /// <summary>
    ///   Persists application preferences through the shared configuration document.
    /// </summary>
    /// <param name="preferences"> The preferences to persist. </param>
    void Save(AppPreferences preferences);
}
