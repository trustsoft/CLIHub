namespace CLIHub;

/// <summary>
///   Opens the single Settings window owned by the running application instance.
/// </summary>
public interface ISettingsLauncher
{
    /// <summary>
    ///   Shows the Settings window, activating the existing instance when it is already open.
    /// </summary>
    void ShowSettings();
}
