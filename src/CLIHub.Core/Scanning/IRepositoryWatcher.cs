namespace CLIHub.Core.Scanning;

/// <summary>
///   Watches a set of root directories for file-system changes.
/// </summary>
public interface IRepositoryWatcher : IDisposable
{
    /// <summary>
    ///   Raised when something under a watched root is created, deleted, or renamed.
    /// </summary>
    event EventHandler? Changed;

    /// <summary>
    ///   Raised when the underlying watcher cannot keep up and a full rescan is required.
    /// </summary>
    event EventHandler? Overflow;

    /// <summary>
    ///   Begins raising change events.
    /// </summary>
    void Start();

}

/// <summary>
///   Creates watchers for a set of root directories.
/// </summary>
public interface IRepositoryWatcherFactory
{
    /// <summary>
    ///   Creates a watcher over the supplied roots.
    /// </summary>
    /// <param name="roots"> The root directories to watch. </param>
    /// <returns> The created watcher. </returns>
    IRepositoryWatcher Create(IReadOnlyList<string> roots);
}
