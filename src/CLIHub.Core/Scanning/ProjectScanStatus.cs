namespace CLIHub.Core.Scanning;

/// <summary>
///   The lifecycle state of the background repository scanner.
/// </summary>
public enum ProjectScanState
{
    /// <summary>
    ///   No watched directories are configured, so nothing is scanned or watched.
    /// </summary>
    NotConfigured,

    /// <summary>
    ///   Watching for changes; no scan is running.
    /// </summary>
    Idle,

    /// <summary>
    ///   A scan is currently running.
    /// </summary>
    Scanning,

    /// <summary>
    ///   The last scan failed.
    /// </summary>
    Error
}

/// <summary>
///   An immutable snapshot of the scanner's state for presentation.
/// </summary>
/// <param name="State"> The lifecycle state. </param>
/// <param name="IsWatching"> Whether watched directories are configured and being observed. </param>
/// <param name="IsScanning"> Whether a scan is currently running. </param>
/// <param name="LastScanUtc"> When the last scan completed, or null if none has run. </param>
/// <param name="DiscoveredCount"> The number of discovered projects registered from the last scan. </param>
/// <param name="ScannedDirectoryCount">
///   The number of directories the last scan examined across the watched roots; the scan coverage.
/// </param>
/// <param name="Error"> The last error message, if the state is <see cref="ProjectScanState.Error" />. </param>
public sealed record ProjectScanStatus(ProjectScanState State,
                                       bool IsWatching,
                                       bool IsScanning,
                                       DateTimeOffset? LastScanUtc,
                                       int DiscoveredCount,
                                       int ScannedDirectoryCount,
                                       string? Error)
{
    /// <summary>
    ///   The status before any watched directory has been configured.
    /// </summary>
    public static ProjectScanStatus NotConfigured { get; } =
        new(ProjectScanState.NotConfigured, false, false, null, 0, 0, null);
}
