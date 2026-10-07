namespace CLIHub;

/// <summary>
///   Application-facing operations needed from the launch window during startup.
/// </summary>
public interface IStartupWindow
{
    /// <summary>
    ///   Shows the window on the monitor containing the pointer.
    /// </summary>
    void ShowOnPointerMonitor();
}
