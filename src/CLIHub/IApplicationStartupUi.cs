namespace CLIHub;

/// <summary>
///   Provides the UI actions and notifications used by application startup orchestration.
/// </summary>
public interface IApplicationStartupUi
{
    /// <summary>
    ///   Raised when the tray or release-notes UI requests an update download.
    /// </summary>
    event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Gets the launch window exposed to the WPF lifecycle adapter.
    /// </summary>
    IStartupWindow LaunchWindow { get; }

    /// <summary>
    ///   Rebuilds the tray menu after update state changes.
    /// </summary>
    void RefreshMenu();

    /// <summary>
    ///   Shows and activates the launch window.
    /// </summary>
    void ShowLaunchWindow();

    /// <summary>
    ///   Shows a notification that an update is available.
    /// </summary>
    /// <param name="version"> The available version. </param>
    void NotifyUpdateAvailable(string version);

    /// <summary>
    ///   Shows a notification that an update was downloaded and the application will restart.
    /// </summary>
    /// <param name="version"> The downloaded version. </param>
    void NotifyUpdateDownloaded(string version);

    /// <summary>
    ///   Shows a notification that an update download failed.
    /// </summary>
    /// <param name="version"> The version whose download failed. </param>
    void NotifyUpdateFailed(string version);
}
