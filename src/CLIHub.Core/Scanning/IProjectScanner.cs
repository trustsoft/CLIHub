namespace CLIHub.Core.Scanning;

/// <summary>
///   Discovers repositories under the watched roots, keeps the project list in sync, and exposes
///   the scanner status.
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
    ///   Starts watching and performs an initial scan. Idempotent while already started.
    /// </summary>
    void Start();

    /// <summary>
    ///   Stops watching and clears the watching state. Idempotent.
    /// </summary>
    void Stop();

    /// <summary>
    ///   Re-reads the scanning preferences, reconfigures the watcher, and triggers a scan. Used
    ///   after the user saves new scanning settings.
    /// </summary>
    void Reload();
}
