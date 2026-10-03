namespace CLIHub;

using CLIHub.Core.Hotkeys;
using CLIHub.Core.Models;

/// <summary>
///   Applies saved preferences to the running application without a restart.
/// </summary>
public interface IPreferenceApplier
{
    /// <summary>
    ///   Applies the launch runtime to the process launcher.
    /// </summary>
    void ApplyRuntime(RuntimeKind runtime);

    /// <summary>
    ///   Applies the launch window's path display style.
    /// </summary>
    void ApplyPathDisplayStyle(PathDisplayStyle style);

    /// <summary>
    ///   Re-registers the global hotkey. Returns false when the new combination could not
    ///   be registered (the previous one is restored in that case).
    /// </summary>
    bool ApplyHotkey(HotkeyDefinition definition);

    /// <summary>
    ///   Applies the startup update-check preference (takes effect on next start).
    /// </summary>
    void ApplyStartupUpdateCheck(bool enabled);

    /// <summary>
    ///   Creates or removes the per-user Windows startup registration.
    ///   Returns false when the registration could not be updated.
    /// </summary>
    bool ApplyStartWithWindows(bool enabled);
}
