namespace CLIHub;

/// <summary>
///   Presents update download outcomes and refreshes the tray menu.
/// </summary>
public interface IUpdateDownloadNotifier
{
    /// <summary>
    ///   Shows that an update was downloaded and is about to restart.
    /// </summary>
    /// <param name="version"> The downloaded version. </param>
    void NotifyDownloaded(string version);

    /// <summary>
    ///   Shows that an update download failed.
    /// </summary>
    /// <param name="version"> The version whose download failed. </param>
    void NotifyFailed(string version);

    /// <summary>
    ///   Refreshes the tray menu state.
    /// </summary>
    void RefreshMenu();
}
