namespace CLIHub.Core.Scanning;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;

/// <summary>
///   Discovers repositories under the watched roots, reconciles the discovered-project subset, and
///   reports status. Scanning runs off the owner thread; project mutations are marshalled back to
///   the owner thread so bound collections update safely.
/// </summary>
public sealed class ProjectScanningService : IProjectScanner
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(750);

    private readonly IPreferencesStore _preferences;
    private readonly IProjectService _projects;
    private readonly IPluginCatalog _plugins;
    private readonly IRepositoryWatcherFactory _watcherFactory;
    private readonly IScanTimerFactory _timerFactory;
    private readonly ILogger<ProjectScanningService> _logger;
    private readonly TimeProvider _time;

    private readonly object _gate = new();
    private readonly SemaphoreSlim _scanGate = new(1, 1);

    private SynchronizationContext? _context;
    private IRepositoryWatcher? _watcher;
    private IScanTimer? _timer;
    private bool _started;
    private bool _disposed;
    private int _scanPending;
    private ProjectScanStatus _status = ProjectScanStatus.NotConfigured;

    /// <summary>
    ///   Creates the scanner.
    /// </summary>
    /// <param name="preferences"> Preferences source for the scanning settings. </param>
    /// <param name="projects"> Project service used to reconcile discovered projects. </param>
    /// <param name="plugins"> Plugin catalog supplying agent project indicators. </param>
    /// <param name="watcherFactory"> Factory for the file-system watcher. </param>
    /// <param name="timerFactory"> Factory for the debounce timer. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="timeProvider"> Optional time source for scan timestamps. </param>
    /// <param name="synchronizationContext"> Optional owner context for tests; otherwise captured at start. </param>
    public ProjectScanningService(
        IPreferencesStore preferences,
        IProjectService projects,
        IPluginCatalog plugins,
        IRepositoryWatcherFactory watcherFactory,
        IScanTimerFactory timerFactory,
        ILogger<ProjectScanningService> logger,
        TimeProvider? timeProvider = null,
        SynchronizationContext? synchronizationContext = null)
    {
        _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
        _watcherFactory = watcherFactory ?? throw new ArgumentNullException(nameof(watcherFactory));
        _timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _time = timeProvider ?? TimeProvider.System;
        _context = synchronizationContext;
    }

    /// <inheritdoc />
    public event EventHandler<ProjectScanStatus>? StatusChanged;

    /// <inheritdoc />
    public ProjectScanStatus Status => _status;

    /// <inheritdoc />
    public void Start()
    {
        lock (_gate)
        {
            if (_disposed || _started)
            {
                return;
            }

            _started = true;
            _context ??= SynchronizationContext.Current;
        }

        Configure();
    }

    /// <inheritdoc />
    public void Reload()
    {
        lock (_gate)
        {
            if (_disposed || !_started)
            {
                return;
            }
        }

        Configure();
    }

    /// <inheritdoc />
    public void Stop()
    {
        IRepositoryWatcher? watcher;
        IScanTimer? timer;

        lock (_gate)
        {
            watcher = _watcher;
            timer = _timer;
            _watcher = null;
            _timer = null;
        }

        Detach(watcher, timer);
        Publish(_status with { State = ProjectScanState.NotConfigured, IsWatching = false, IsScanning = false });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IRepositoryWatcher? watcher;
        IScanTimer? timer;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _started = false;
            watcher = _watcher;
            timer = _timer;
            _watcher = null;
            _timer = null;
        }

        Detach(watcher, timer);
        _scanGate.Dispose();
    }

    private void Configure()
    {
        var options = ProjectScanningOptions.From(_preferences.Load());

        IRepositoryWatcher? oldWatcher;
        IScanTimer? oldTimer;
        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            oldWatcher = _watcher;
            oldTimer = _timer;
            _watcher = null;
            _timer = null;
        }

        Detach(oldWatcher, oldTimer);

        if (options.WatchedDirectories.Count == 0)
        {
            Publish(_status with { State = ProjectScanState.NotConfigured, IsWatching = false, IsScanning = false });

            // Still reconcile so discovered projects under removed roots are cleaned up.
            RequestScan();
            return;
        }

        var timer = _timerFactory.Create();
        timer.Elapsed += OnDebounceElapsed;
        var watcher = _watcherFactory.Create(options.WatchedDirectories);
        watcher.Changed += OnWatcherChanged;
        watcher.Overflow += OnWatcherOverflow;
        watcher.Start();

        lock (_gate)
        {
            if (_disposed)
            {
                Detach(watcher, timer);
                return;
            }

            _timer = timer;
            _watcher = watcher;
        }

        Publish(_status with { State = ProjectScanState.Idle, IsWatching = true, IsScanning = false });
        RequestScan();
    }

    private void Detach(IRepositoryWatcher? watcher, IScanTimer? timer)
    {
        if (watcher is not null)
        {
            watcher.Changed -= OnWatcherChanged;
            watcher.Overflow -= OnWatcherOverflow;
            watcher.Dispose();
        }

        if (timer is not null)
        {
            timer.Elapsed -= OnDebounceElapsed;
            timer.Dispose();
        }
    }

    private void OnWatcherChanged(object? sender, EventArgs e)
    {
        lock (_gate)
        {
            _timer?.Arm(DebounceDelay);
        }
    }

    private void OnDebounceElapsed(object? sender, EventArgs e) => RequestScan();

    private void OnWatcherOverflow(object? sender, EventArgs e)
    {
        _logger.LogWarning("Repository watcher buffer overflow; performing a full rescan");
        Configure();
    }

    private void RequestScan()
    {
        if (Interlocked.Exchange(ref _scanPending, 1) == 1)
        {
            return;
        }

        _ = ScanLoopAsync();
    }

    private async Task ScanLoopAsync()
    {
        while (Interlocked.Exchange(ref _scanPending, 0) == 1)
        {
            try
            {
                await _scanGate.WaitAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            try
            {
                await RunScanAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project scan failed");
                Publish(_status with { State = ProjectScanState.Error, IsScanning = false, Error = ex.Message });
            }
            finally
            {
                _scanGate.Release();
            }
        }
    }

    private async Task RunScanAsync()
    {
        var options = ProjectScanningOptions.From(_preferences.Load());
        var plugins = _plugins.GetAllPlugins();
        var configured = options.WatchedDirectories.Count > 0;

        Publish(_status with
        {
            State = configured ? ProjectScanState.Scanning : ProjectScanState.NotConfigured,
            IsWatching = configured,
            IsScanning = configured,
            Error = null
        });

        var result = await Task.Run(() => RepositoryDiscovery.Enumerate(
            options,
            plugins,
            (root, ex) => _logger.LogWarning(ex, "Could not scan watched directory {Root}", root))).ConfigureAwait(false);

        Post(() =>
        {
            var removed = ComputeRemovals(result.DiscoveredPaths, options.WatchedDirectories, result.ScannedRoots);
            _projects.ReconcileDiscoveredProjects(result.DiscoveredPaths, removed);

            Publish(_status with
            {
                State = configured ? ProjectScanState.Idle : ProjectScanState.NotConfigured,
                IsWatching = configured,
                IsScanning = false,
                LastScanUtc = _time.GetUtcNow(),
                DiscoveredCount = result.DiscoveredPaths.Count,
                Error = null
            });
        });
    }

    private IReadOnlyList<string> ComputeRemovals(
        IReadOnlyList<string> discoveredPaths,
        IReadOnlyList<string> watchedRoots,
        IReadOnlyList<string> scannedRoots)
    {
        var existing = _projects.GetAllProjects();
        var discovered = new HashSet<string>(discoveredPaths.Select(ProjectPathPolicy.Normalize), StringComparer.OrdinalIgnoreCase);
        var roots = watchedRoots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .Select(ProjectPathPolicy.Normalize)
            .ToArray();
        var scanned = scannedRoots.Select(ProjectPathPolicy.Normalize).ToArray();

        var removals = new List<string>();

        foreach (var project in existing)
        {
            if (project.Source != ProjectSource.Discovered)
            {
                continue;
            }

            var path = ProjectPathPolicy.Normalize(project.Path);

            if (!roots.Any(root => ProjectPathPolicy.IsUnder(path, root)))
            {
                removals.Add(project.Id);
                continue;
            }

            if (scanned.Any(root => ProjectPathPolicy.IsUnder(path, root)) && !discovered.Contains(path))
            {
                removals.Add(project.Id);
            }
        }

        return removals;
    }

    private void Publish(ProjectScanStatus status) => Post(() =>
    {
        _status = status;
        StatusChanged?.Invoke(this, status);
    });

    private void Post(Action action)
    {
        var context = _context;

        if (context is null || context == SynchronizationContext.Current)
        {
            action();
            return;
        }

        context.Post(_ => action(), null);
    }
}
