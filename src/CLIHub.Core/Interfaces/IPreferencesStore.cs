namespace CLIHub.Core.Interfaces;

using CLIHub.Core.Models;

/// <summary>
///   Provides access to application preferences without exposing project state.
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
