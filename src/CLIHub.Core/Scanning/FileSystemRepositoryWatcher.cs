namespace CLIHub.Core.Scanning;

using Microsoft.Extensions.Logging;

/// <summary>
///   Default <see cref="IRepositoryWatcherFactory"/> backed by <see cref="FileSystemWatcher"/>.
/// </summary>
public sealed class FileSystemRepositoryWatcherFactory : IRepositoryWatcherFactory
{
    private readonly ILogger<FileSystemRepositoryWatcher>? _logger;

    /// <summary>
    ///   Creates the factory.
    /// </summary>
    /// <param name="logger"> Optional logger for watcher setup and disposal issues. </param>
    public FileSystemRepositoryWatcherFactory(ILogger<FileSystemRepositoryWatcher>? logger = null) =>
        _logger = logger;

    /// <inheritdoc />
    public IRepositoryWatcher Create(IReadOnlyList<string> roots) =>
        new FileSystemRepositoryWatcher(roots, _logger);
}

/// <summary>
///   Watches each root with a recursive <see cref="FileSystemWatcher"/>.
/// </summary>
public sealed class FileSystemRepositoryWatcher : IRepositoryWatcher
{
    private const int BufferSize = 64 * 1024;

    private readonly List<FileSystemWatcher> _watchers = new();
    private bool _disposed;

    /// <summary>
    ///   Creates a watcher over the supplied roots. Roots that do not exist are skipped.
    /// </summary>
    /// <param name="roots"> The root directories to watch. </param>
    /// <param name="logger"> Optional logger. </param>
    public FileSystemRepositoryWatcher(IReadOnlyList<string> roots, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(roots);

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
            {
                logger?.LogWarning("Not watching missing root {Root}", root);
                continue;
            }

            try
            {
                var watcher = new FileSystemWatcher(root)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.CreationTime,
                    InternalBufferSize = BufferSize
                };

                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;
                watcher.Error += OnError;
                _watchers.Add(watcher);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Could not watch root {Root}", root);
            }
        }
    }

    /// <inheritdoc />
    public event EventHandler? Changed;

    /// <inheritdoc />
    public event EventHandler? Overflow;

    /// <inheritdoc />
    public void Start()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = true;
        }
    }

    /// <inheritdoc />
    public void Stop()
    {
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var watcher in _watchers)
        {
            watcher.Created -= OnChanged;
            watcher.Deleted -= OnChanged;
            watcher.Renamed -= OnRenamed;
            watcher.Error -= OnError;
            watcher.Dispose();
        }

        _watchers.Clear();
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnRenamed(object sender, RenamedEventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnError(object sender, ErrorEventArgs e) => Overflow?.Invoke(this, EventArgs.Empty);
}
