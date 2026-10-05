namespace CLIHub.Core.Updates;

using CLIHub.Core.Models;

/// <summary>
///   Pure state transitions for the launch window's footer update control, derived from the
///   facts shared by <c>IUpdateService</c>. Side-effect-free so tests cover every transition.
/// </summary>
public static class UpdateControlLogic
{
    /// <summary>
    ///   Derives the control state from the update service facts. States the control drives
    ///   itself (checking, or a completed download awaiting restart) are never overridden by
    ///   an external refresh.
    /// </summary>
    /// <param name="current"> The state the control is currently in. </param>
    /// <param name="isDownloading"> Whether the update service is downloading right now. </param>
    /// <param name="isUpdateAvailable"> Whether the update service knows an available version. </param>
    /// <returns> The state the control should display. </returns>
    public static UpdateControlState Derive(UpdateControlState current, bool isDownloading, bool isUpdateAvailable)
    {
        if (current is UpdateControlState.Checking or UpdateControlState.ReadyToApply)
        {
            return current;
        }

        if (isDownloading)
        {
            return UpdateControlState.Downloading;
        }

        return isUpdateAvailable ? UpdateControlState.Available : UpdateControlState.Idle;
    }

    /// <summary>
    ///   Derives the state after a download request completes.
    /// </summary>
    /// <param name="status"> The download outcome reported by the update service. </param>
    /// <param name="isDownloading"> Whether a download is still running (for example one started elsewhere). </param>
    /// <param name="isUpdateAvailable"> Whether the update service knows an available version. </param>
    /// <returns> Ready to apply after a successful download; otherwise the state derived from the service facts. </returns>
    public static UpdateControlState AfterDownload(UpdateDownloadStatus status, bool isDownloading, bool isUpdateAvailable) =>
        status == UpdateDownloadStatus.Downloaded
            ? UpdateControlState.ReadyToApply
            : Derive(UpdateControlState.Idle, isDownloading, isUpdateAvailable);
}
