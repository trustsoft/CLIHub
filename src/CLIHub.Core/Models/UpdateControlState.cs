namespace CLIHub.Core.Models;

/// <summary>
///   Display states of the launch window's footer update control.
/// </summary>
public enum UpdateControlState
{
    /// <summary>
    ///   Shows the current version; clicking triggers an update check.
    /// </summary>
    Idle,

    /// <summary>
    ///   An update check is running.
    /// </summary>
    Checking,

    /// <summary>
    ///   A newer version is available; clicking starts the download.
    /// </summary>
    Available,

    /// <summary>
    ///   An update download is running.
    /// </summary>
    Downloading,

    /// <summary>
    ///   The download completed; clicking applies the update and restarts.
    /// </summary>
    ReadyToApply,
}
