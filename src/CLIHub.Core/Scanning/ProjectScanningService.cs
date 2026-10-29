namespace CLIHub.Core.Scanning;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;

/// <summary>
///   Discovers repositories under the watched roots, reconciles the discovered-project subset, and
///   reports status. Scanning runs off the owner thread; project mutations are marshaled back to
///   the owner thread so bound collections update safely.
/// </summary>
public sealed class ProjectScanningService : IProjectScanner
{
    private static readonly TimeSpan DebounceDelay = TimeSpan.FromMilliseconds(750);

    private readonly IPreferencesStore preferences;
    private readonly IProjectService projects;
    private readonly IPluginCatalog plugins;
    private readonly IRepositoryWatcherFactory watcherFactory;
    private readonly IScanTimerFactory timerFactory;
    private readonly ILoggingContextProvider loggingContext;
    private readonly ILogger<ProjectScanningService> logger;
    private readonly TimeProvider time;

    private readonly object gate = new();
    private readonly SemaphoreSlim scanGate = new(1, 1);
    private readonly Dictionary<string, int> scannedDirectoryCounts = new(StringComparer.OrdinalIgnoreCase);

    private SynchronizationContext? context;
    private IRepositoryWatcher? watcher;
    private IScanTimer? timer;
    private bool started;
    private bool disposed;
    private int scanPending;
    private ProjectScanStatus status = ProjectScanStatus.NotConfigured;

    /// <summary>
    ///   Creates the scanner.
    /// </summary>
    /// <param name="preferences"> Preferences source for the scanning settings. </param>
    /// <param name="projects"> Project service used to reconcile discovered projects. </param>
    /// <param name="plugins"> Plugin catalog supplying agent project indicators. </param>
    /// <param name="watcherFactory"> Factory for the file-system watcher. </param>
    /// <param name="timerFactory"> Factory for the debounce timer. </param>
    /// <param name="loggingContext"> Logging context provider. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="timeProvider"> Optional time source for scan timestamps. </param>
    /// <param name="synchronizationContext"> Optional owner context for tests; otherwise captured at start. </param>
    public ProjectScanningService(IPreferencesStore preferences,
                                  IProjectService projects,
                                  IPluginCatalog plugins,
                                  IRepositoryWatcherFactory watcherFactory,
                                  IScanTimerFactory timerFactory,
                                  ILoggingContextProvider loggingContext,
                                  ILogger<ProjectScanningService> logger,
                                  TimeProvider? timeProvider = null,
                                  SynchronizationContext? synchronizationContext = null)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
        this.watcherFactory = watcherFactory ?? throw new ArgumentNullException(nameof(watcherFactory));
        this.timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
        this.loggingContext = loggingContext ?? throw new ArgumentNullException(nameof(loggingContext));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.time = timeProvider ?? TimeProvider.System;
        this.context = synchronizationContext;
    }

    /// <inheritdoc />
    public event EventHandler<ProjectScanStatus>? StatusChanged;

    /// <inheritdoc />
    public ProjectScanStatus Status => this.status;

    /// <inheritdoc />
    public int CountDiscoveredProjects(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return 0;
        }

        try
        {
            return this.projects.GetAllProjects()
                .Count(project => project.Source == ProjectSource.Discovered
                                  && ProjectPathPolicy.IsUnder(project.Path, root));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A malformed stored root must not break the page that displays its count.
            return 0;
        }
    }

    /// <inheritdoc />
    public int CountManualProjects() =>
        this.projects.GetAllProjects().Count(project => project.Source == ProjectSource.Manual);

    /// <inheritdoc />
    public void Start()
    {
        lock (this.gate)
        {
            if (this.disposed || this.started)
            {
                return;
            }

            this.started = true;
            this.context ??= SynchronizationContext.Current;
        }

        this.Configure();
    }

    /// <inheritdoc />
    public void Reload()
    {
        lock (this.gate)
        {
            if (this.disposed || !this.started)
            {
                return;
            }
        }

        this.Configure();
    }

    /// <inheritdoc />
    public void Rescan(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        lock (this.gate)
        {
            if (this.disposed || !this.started)
            {
                return;
            }
        }

        _ = this.RescanRootAsync(root);
    }

    /// <inheritdoc />
    public void Stop()
    {
        IRepositoryWatcher? watcher;
        IScanTimer? timer;

        lock (this.gate)
        {
            watcher = this.watcher;
            timer = this.timer;
            this.watcher = null;
            this.timer = null;
        }

        this.Detach(watcher, timer);
        this.Post(() =>
        {
            this.scannedDirectoryCounts.Clear();
            this.Publish(this.status with
            {
                State = ProjectScanState.NotConfigured,
                IsWatching = false,
                IsScanning = false,
                ScannedDirectoryCount = 0
            });
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        IRepositoryWatcher? watcher;
        IScanTimer? timer;

        lock (this.gate)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
            this.started = false;
            watcher = this.watcher;
            timer = this.timer;
            this.watcher = null;
            this.timer = null;
        }

        this.Detach(watcher, timer);
        this.scanGate.Dispose();
    }

    private void Configure()
    {
        var options = ProjectScanningOptions.From(this.preferences.Load().Scanning);

        IRepositoryWatcher? oldWatcher;
        IScanTimer? oldTimer;
        lock (this.gate)
        {
            if (this.disposed)
            {
                return;
            }

            oldWatcher = this.watcher;
            oldTimer = this.timer;
            this.watcher = null;
            this.timer = null;
        }

        this.Detach(oldWatcher, oldTimer);

        if (options.WatchedDirectories.Count == 0)
        {
            this.Post(() =>
            {
                this.scannedDirectoryCounts.Clear();
                this.Publish(this.status with
                {
                    State = ProjectScanState.NotConfigured,
                    IsWatching = false,
                    IsScanning = false,
                    ScannedDirectoryCount = 0
                });
            });

            // Still reconcile so discovered projects under removed roots are cleaned up.
            this.RequestScan();
            return;
        }

        var timer = this.timerFactory.Create();
        timer.Elapsed += this.OnDebounceElapsed;
        var watcher = this.watcherFactory.Create(options.WatchedDirectories);
        watcher.Changed += this.OnWatcherChanged;
        watcher.Overflow += this.OnWatcherOverflow;
        watcher.Start();

        lock (this.gate)
        {
            if (this.disposed)
            {
                this.Detach(watcher, timer);
                return;
            }

            this.timer = timer;
            this.watcher = watcher;
        }

        this.Post(() =>
        {
            this.PruneScannedDirectoryCounts(options.WatchedDirectories);
            this.Publish(this.status with
            {
                State = ProjectScanState.Idle,
                IsWatching = true,
                IsScanning = false,
                ScannedDirectoryCount = this.scannedDirectoryCounts.Values.Sum()
            });
        });
        this.RequestScan();
    }

    private void PruneScannedDirectoryCounts(IReadOnlyList<string> watchedRoots)
    {
        var configured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in watchedRoots)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            try
            {
                configured.Add(ProjectPathPolicy.Normalize(root));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // A malformed root has no coverage entry to keep.
            }
        }

        foreach (var root in this.scannedDirectoryCounts.Keys.Where(key => !configured.Contains(key)).ToArray())
        {
            this.scannedDirectoryCounts.Remove(root);
        }
    }

    private void Detach(IRepositoryWatcher? watcher, IScanTimer? timer)
    {
        if (watcher is not null)
        {
            watcher.Changed -= this.OnWatcherChanged;
            watcher.Overflow -= this.OnWatcherOverflow;
            watcher.Dispose();
        }

        if (timer is not null)
        {
            timer.Elapsed -= this.OnDebounceElapsed;
            timer.Dispose();
        }
    }

    private void OnWatcherChanged(object? sender, EventArgs e)
    {
        lock (this.gate)
        {
            this.timer?.Arm(DebounceDelay);
        }
    }

    private void OnDebounceElapsed(object? sender, EventArgs e) => this.RequestScan();

    private void OnWatcherOverflow(object? sender, EventArgs e)
    {
        this.logger.LogWarning("Repository watcher buffer overflow; performing a full rescan");
        this.Configure();
    }

    private void RequestScan()
    {
        if (Interlocked.Exchange(ref this.scanPending, 1) == 1)
        {
            return;
        }

        _ = this.ScanLoopAsync();
    }

    private async Task ScanLoopAsync()
    {
        while (Interlocked.Exchange(ref this.scanPending, 0) == 1)
        {
            try
            {
                await this.scanGate.WaitAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            try
            {
                await this.RunScanAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Project scan failed");
                this.Publish(this.status with { State = ProjectScanState.Error, IsScanning = false, Error = ex.Message });
            }
            finally
            {
                this.scanGate.Release();
            }
        }
    }

    private async Task RunScanAsync()
    {
        using var scope = this.loggingContext.BeginOperation("ProjectScan");

        var options = ProjectScanningOptions.From(this.preferences.Load().Scanning);
        var plugins = this.plugins.GetAllPlugins();
        var configured = options.WatchedDirectories.Count > 0;

        this.Publish(this.status with
        {
            State = configured ? ProjectScanState.Scanning : ProjectScanState.NotConfigured,
            IsWatching = configured,
            IsScanning = configured,
            Error = null
        });

        var result = await Task.Run(() => RepositoryDiscovery.Enumerate(
            options,
            plugins,
            (root, ex) => this.logger.LogWarning(ex, "Could not scan watched directory {Root}", root))).ConfigureAwait(false);

        this.Post(() =>
        {
            var removed = this.ComputeRemovals(result.DiscoveredPaths, options.WatchedDirectories, result.ScannedRoots);
            this.projects.ReconcileDiscoveredProjects(result.DiscoveredPaths, removed);

            this.scannedDirectoryCounts.Clear();
            foreach (var (scannedRoot, examined) in result.ScannedDirectoryCounts)
            {
                this.scannedDirectoryCounts[scannedRoot] = examined;
            }

            // Count registered discovered projects, not this pass's paths: repositories that were
            // already registered manually keep their provenance and must not inflate the count, and
            // a single-root pass uses the same definition.
            var discoveredCount = this.projects.GetAllProjects()
                .Count(project => project.Source == ProjectSource.Discovered);

            this.Publish(this.status with
            {
                State = configured ? ProjectScanState.Idle : ProjectScanState.NotConfigured,
                IsWatching = configured,
                IsScanning = false,
                LastScanUtc = this.time.GetUtcNow(),
                DiscoveredCount = discoveredCount,
                ScannedDirectoryCount = this.scannedDirectoryCounts.Values.Sum(),
                Error = null
            });
        });
    }

    private async Task RescanRootAsync(string root)
    {
        try
        {
            await this.scanGate.WaitAsync().ConfigureAwait(false);
        }
        catch (ObjectDisposedException)
        {
            return;
        }

        try
        {
            await this.RunRootScanAsync(root).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Project scan failed for watched directory {Root}", root);
            this.Publish(this.status with { State = ProjectScanState.Error, IsScanning = false, Error = ex.Message });
        }
        finally
        {
            this.scanGate.Release();
        }
    }

    private async Task RunRootScanAsync(string root)
    {
        using var scope = this.loggingContext.BeginOperation("ProjectScan");

        var options = ProjectScanningOptions.From(this.preferences.Load().Scanning);
        var rootOptions = options.ForRoot(root) with { WatchedDirectories = new[] { root } };
        var plugins = this.plugins.GetAllPlugins();

        this.Publish(this.status with
        {
            State = ProjectScanState.Scanning,
            IsScanning = true,
            Error = null
        });

        var result = await Task.Run(() => RepositoryDiscovery.Enumerate(
            rootOptions,
            plugins,
            (candidate, ex) => this.logger.LogWarning(ex, "Could not scan watched directory {Root}", candidate))).ConfigureAwait(false);

        this.Post(() =>
        {
            var removed = this.ComputeRootRemovals(root, result.DiscoveredPaths, result.ScannedRoots, options.WatchedDirectories);
            this.projects.ReconcileDiscoveredProjects(result.DiscoveredPaths, removed);

            foreach (var (scannedRoot, examined) in result.ScannedDirectoryCounts)
            {
                this.scannedDirectoryCounts[scannedRoot] = examined;
            }

            var discoveredCount = this.projects.GetAllProjects()
                .Count(project => project.Source == ProjectSource.Discovered);

            this.Publish(this.status with
            {
                State = this.status.IsWatching ? ProjectScanState.Idle : ProjectScanState.NotConfigured,
                IsScanning = false,
                LastScanUtc = this.time.GetUtcNow(),
                DiscoveredCount = discoveredCount,
                ScannedDirectoryCount = this.scannedDirectoryCounts.Values.Sum(),
                Error = null
            });
        });
    }

    private IReadOnlyList<string> ComputeRootRemovals(string root,
                                                      IReadOnlyList<string> discoveredPaths,
                                                      IReadOnlyList<string> scannedRoots,
                                                      IReadOnlyList<string> watchedRoots)
    {
        // A missing or unreadable root scans nothing, so it must not be treated as "all projects vanished".
        if (scannedRoots.Count == 0)
        {
            return Array.Empty<string>();
        }

        var discovered = new HashSet<string>(
            discoveredPaths.Select(ProjectPathPolicy.Normalize),
            StringComparer.OrdinalIgnoreCase);
        var otherRoots = watchedRoots
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate))
            .Where(candidate => !ProjectPathPolicy.AreEqual(candidate, root))
            .Select(ProjectPathPolicy.Normalize)
            .ToArray();

        var removals = new List<string>();

        foreach (var project in this.projects.GetAllProjects())
        {
            if (project.Source != ProjectSource.Discovered)
            {
                continue;
            }

            var path = ProjectPathPolicy.Normalize(project.Path);

            if (!ProjectPathPolicy.IsUnder(path, root))
            {
                continue;
            }

            // The other root was not part of this pass, so it may still contain the project.
            if (otherRoots.Any(other => ProjectPathPolicy.IsUnder(path, other)))
            {
                continue;
            }

            if (!discovered.Contains(path))
            {
                removals.Add(project.Id);
            }
        }

        return removals;
    }

    private IReadOnlyList<string> ComputeRemovals(IReadOnlyList<string> discoveredPaths,
                                                  IReadOnlyList<string> watchedRoots,
                                                  IReadOnlyList<string> scannedRoots)
    {
        var existing = this.projects.GetAllProjects();
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

    private void Publish(ProjectScanStatus status) => this.Post(() =>
    {
        this.status = status;
        this.StatusChanged?.Invoke(this, status);
    });

    private void Post(Action action)
    {
        var context = this.context;

        if (context is null || context == SynchronizationContext.Current)
        {
            action();
            return;
        }

        context.Post(_ => action(), null);
    }
}
