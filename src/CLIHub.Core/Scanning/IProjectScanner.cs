namespace CLIHub.Core.Scanning;

/// <summary>
///   Discovers repositories under the watched roots,
///   keeps the project list in sync, and exposes the scanner status.
/// </summary>
public interface IProjectScanner : IDisposable
{
    /// <summary>
    ///   Raised whenever the scanner status changes.
    /// </summary>
    event EventHandler<ProjectScanStatus>? StatusChanged;

    /// <summary>
    ///   The current scanner status.
    /// </summary>
    ProjectScanStatus Status { get; }

    /// <summary>
    ///   Returns the number of discovered projects registered beneath a watched root.
    /// </summary>
    /// <param name="root"> The watched root. </param>
    /// <returns> The count of discovered projects at or below the root. </returns>
    int CountDiscoveredProjects(string root);

    /// <summary>
    ///   Starts watching and performs an initial scan. Idempotent while already started.
    /// </summary>
    void Start();

    /// <summary>
    ///   Stops watching and clears the watching state. Idempotent.
    /// </summary>
    void Stop();

    /// <summary>
    ///   Re-reads the scanning preferences, reconfigures the watcher,
    ///   and triggers a scan. Used after the user saves new scanning settings.
    /// </summary>
    void Reload();

    /// <summary>
    ///   Rescans one watched root without touching the other roots.
    ///   Used by the per-directory scan action on the Projects &amp; Paths page.
    /// </summary>
    /// <param name="root"> The watched root to rescan. </param>
    void Rescan(string root);
}
