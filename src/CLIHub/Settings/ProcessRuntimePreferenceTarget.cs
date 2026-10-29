namespace CLIHub.Settings;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Adapts the preferences store to the narrow runtime preference port.
/// </summary>
public sealed class ProcessRuntimePreferenceTarget : IRuntimePreferenceTarget
{
    private readonly IPreferencesStore preferencesStore;

    /// <summary>
    ///   Creates the runtime preference target.
    /// </summary>
    /// <param name="preferencesStore"> Preferences store for persisting the runtime. </param>
    public ProcessRuntimePreferenceTarget(IPreferencesStore preferencesStore)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
    }

    /// <inheritdoc />
    public void SetRuntime(RuntimeKind runtime)
    {
        var preferences = this.preferencesStore.Load();
        preferences.DefaultRuntime = runtime;
        this.preferencesStore.Update(_ => _.DefaultRuntime = runtime);
    }
}
