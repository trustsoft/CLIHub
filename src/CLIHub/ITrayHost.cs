namespace CLIHub;

/// <summary>
///   UI operations exposed by the system tray host to application coordinators.
/// </summary>
public interface ITrayHost
{
    /// <summary> Shows a downloaded update notification. </summary>
    void NotifyUpdateDownloaded(string version);

    /// <summary> Shows a failed update notification. </summary>
    void NotifyUpdateFailed(string version);

    /// <summary> Rebuilds the tray menu. </summary>
    void RefreshMenu();
}
