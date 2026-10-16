namespace CLIHub.Core.Scanning;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
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
    private readonly ILogger<ProjectScanningService> logger;
    private readonly TimeProvider time;

    private readonly object gate = new();
    private readonly SemaphoreSlim scanGate = new(1, 1);

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
    /// <param name="logger"> Logger. </param>
    /// <param name="timeProvider"> Optional time source for scan timestamps. </param>
    /// <param name="synchronizationContext"> Optional owner context for tests; otherwise captured at start. </param>
    public ProjectScanningService(IPreferencesStore preferences,
                                  IProjectService projects,
                                  IPluginCatalog plugins,
                                  IRepositoryWatcherFactory watcherFactory,
                                  IScanTimerFactory timerFactory,
                                  ILogger<ProjectScanningService> logger,
                                  TimeProvider? timeProvider = null,
                                  SynchronizationContext? synchronizationContext = null)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));
        this.watcherFactory = watcherFactory ?? throw new ArgumentNullException(nameof(watcherFactory));
        this.timerFactory = timerFactory ?? throw new ArgumentNullException(nameof(timerFactory));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.time = timeProvider ?? TimeProvider.System;
        this.context = synchronizationContext;
    }

    /// <inheritdoc />
    public event EventHandler<ProjectScanStatus>? StatusChanged;

    /// <inheritdoc />
    public ProjectScanStatus Status => this.status;

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
        this.Publish(this.status with { State = ProjectScanState.NotConfigured, IsWatching = false, IsScanning = false });
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
        var options = ProjectScanningOptions.From(this.preferences.Load());

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
            this.Publish(this.status with { State = ProjectScanState.NotConfigured, IsWatching = false, IsScanning = false });

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

        this.Publish(this.status with { State = ProjectScanState.Idle, IsWatching = true, IsScanning = false });
        this.RequestScan();
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
        var options = ProjectScanningOptions.From(this.preferences.Load());
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

            this.Publish(this.status with
            {
                State = configured ? ProjectScanState.Idle : ProjectScanState.NotConfigured,
                IsWatching = configured,
                IsScanning = false,
                LastScanUtc = this.time.GetUtcNow(),
                DiscoveredCount = result.DiscoveredPaths.Count,
                Error = null
            });
        });
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
