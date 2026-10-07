namespace CLIHub;

/// <summary>
///   Supplies tray menu state and application actions without owning WPF tray controls.
/// </summary>
public interface ITrayActions
{
    /// <summary>
    ///   Raised when the state projected into the tray menu changes.
    /// </summary>
    event EventHandler? StateChanged;

    /// <summary>
    ///   Raised when the tray requests downloading an available update.
    /// </summary>
    event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Gets the current menu state.
    /// </summary>
    TrayMenuState GetState();

    /// <summary>
    ///   Gets commands for the current menu projection.
    /// </summary>
    TrayMenuCommands Commands { get; }
}
