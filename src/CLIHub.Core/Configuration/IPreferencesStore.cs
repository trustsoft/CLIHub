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
    ///   Updates detached preferences and persists them when the callback completes successfully.
    /// </summary>
    /// <param name="update"> Mutation applied to a detached preferences value. </param>
    void Update(Action<AppPreferences> update);
}
