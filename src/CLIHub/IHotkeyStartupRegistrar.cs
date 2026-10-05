namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Applies and registers the global hotkey during application startup.
/// </summary>
public interface IHotkeyStartupRegistrar
{
    /// <summary>
    ///   Parses the configured hotkey, applies the default when invalid, and registers it.
    /// </summary>
    /// <param name="preferences"> The preferences loaded for this application start. </param>
    void Register(AppPreferences preferences);
}
