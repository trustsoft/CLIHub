namespace CLIHub.Helpers;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Helper for synchronizing ViewModel properties with preference store.
/// </summary>
public static class PreferenceSyncHelper
{
    /// <summary>
    ///   Updates a preference in the store and optionally invokes a callback.
    /// </summary>
    /// <param name="preferencesStore"> The preference store to update. </param>
    /// <param name="updatePreference"> Action to update the preference. </param>
    /// <param name="onChanged"> Optional callback invoked after preference update. </param>
    /// <exception cref="ArgumentNullException"> Thrown when required parameters are null. </exception>
    public static void SyncPreference(
        IPreferencesStore preferencesStore,
        Action<AppPreferences> updatePreference,
        Action? onChanged = null)
    {
        ArgumentNullException.ThrowIfNull(preferencesStore);
        ArgumentNullException.ThrowIfNull(updatePreference);

        preferencesStore.Update(updatePreference);
        onChanged?.Invoke();
    }
}
