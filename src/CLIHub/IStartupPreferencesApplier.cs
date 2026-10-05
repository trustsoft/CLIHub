namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Applies preferences that affect application startup.
/// </summary>
public interface IStartupPreferencesApplier
{
    /// <summary>
    ///   Applies the configured process runtime and Windows startup registration.
    /// </summary>
    /// <param name="preferences"> The preferences loaded for this application start. </param>
    void Apply(AppPreferences preferences);
}
