namespace CLIHub.Core.Tests.Scanning;

using System.Diagnostics;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Scanning;
using CLIHub.Core.Tests.Fakes;

public class ProjectScanningServiceTests : IDisposable
{
    private readonly List<string> tempDirs = new();
    private readonly FakeRepositoryWatcherFactory watcherFactory = new();
    private readonly FakeScanTimerFactory timerFactory = new();
    private readonly AppPreferences preferences = new();
    private readonly List<Project> projects = new();
    private readonly List<(IReadOnlyCollection<string> Discovered, IReadOnlyCollection<string> Removed)> reconciliations = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-scan-svc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        this.tempDirs.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (var dir in this.tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private ProjectScanningService CreateService(
        Action<IReadOnlyCollection<string>, IReadOnlyCollection<string>>? onReconciled = null)
    {
        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(() => this.preferences);

        var projects = new Mock<IProjectService>();
        projects.Setup(x => x.GetAllProjects()).Returns(() => this.projects.ToList());
        projects
            .Setup(x => x.ReconcileDiscoveredProjects(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Callback<IReadOnlyCollection<string>, IReadOnlyCollection<string>>((discovered, removed) =>
            {
                this.reconciliations.Add((discovered, removed));
                onReconciled?.Invoke(discovered, removed);
            })
            .Returns(false);

        var plugins = new Mock<IPluginCatalog>();
        plugins.Setup(x => x.GetAllPlugins()).Returns([]);

        return new ProjectScanningService(
            preferences.Object,
            projects.Object,
            plugins.Object,
            this.watcherFactory,
            this.timerFactory,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            NullLogger<ProjectScanningService>.Instance,
            new FakeTimeProvider(new DateTimeOffset(2026, 10, 11, 12, 0, 0, TimeSpan.Zero)),
            new InlineSynchronizationContext());
    }

    private static bool WaitFor(Func<bool> condition, int timeoutMs = 5000)
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds < timeoutMs)
        {
            if (condition())
            {
                return true;
            }

            Thread.Sleep(10);
        }

        return condition();
    }

    [Fact]
    public void Start_NoWatchedDirectories_DoesNotWatch()
    {
        var service = this.CreateService();

        service.Start();

        Assert.Equal(ProjectScanState.NotConfigured, service.Status.State);
        Assert.Empty(this.watcherFactory.CreatedRoots);
    }

    [Fact]
    public void Start_WithWatchedDirectory_WatchesAndReconcilesDiscoveredRepository()
    {
        var root = this.CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        this.preferences.WatchedDirectories = [root];
        this.preferences.ScanDepth = 2;
        this.preferences.DiscoveryMode = ProjectDiscoveryMode.Both;

        var service = this.CreateService();
        service.Start();

        Assert.True(WaitFor(() => this.reconciliations.Count > 0));
        Assert.Single(this.watcherFactory.Watchers);
        Assert.True(this.watcherFactory.Watchers[0].Started);
        Assert.Contains(repo, this.reconciliations[^1].Discovered);
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && service.Status.IsWatching));
    }

    [Fact]
    public void Stop_DisposesWatcherAndClearsStatus()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        this.preferences.WatchedDirectories = [root];
        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => this.watcherFactory.Watchers.Count == 1
                             && service.Status.State == ProjectScanState.Idle
                             && service.Status.ScannedDirectoryCount == 2));

        service.Stop();

        Assert.True(this.watcherFactory.Watchers[0].Disposed);
        Assert.Equal(ProjectScanState.NotConfigured, service.Status.State);
    }

    [Fact]
    public void Status_DiscoveredCount_MatchesBetweenFullAndSingleRootPasses()
    {
        var root = this.CreateTempDir();
        var manualRepo = Path.Combine(root, "manual");
        Directory.CreateDirectory(Path.Combine(manualRepo, ".git"));
        var discoveredRepo = Path.Combine(root, "discovered");
        Directory.CreateDirectory(Path.Combine(discoveredRepo, ".git"));
        this.projects.Add(new Project
        {
            Id = "manual",
            Name = "manual",
            Path = manualRepo,
            Source = ProjectSource.Manual
        });
        this.preferences.WatchedDirectories = [root];

        var service = this.CreateService((discovered, removed) =>
        {
            foreach (var path in discovered)
            {
                if (this.projects.Any(project => string.Equals(project.Path, path, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                this.projects.Add(new Project
                {
                    Id = path,
                    Name = Path.GetFileName(path),
                    Path = path,
                    Source = ProjectSource.Discovered
                });
            }

            foreach (var id in removed)
            {
                this.projects.RemoveAll(project => project.Id == id);
            }
        });

        service.Start();
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && this.reconciliations.Count > 0));
        Assert.Equal(1, service.Status.DiscoveredCount);

        var passes = this.reconciliations.Count;
        service.Rescan(root);
        Assert.True(WaitFor(() => this.reconciliations.Count > passes));
        Assert.Equal(1, service.Status.DiscoveredCount);
    }

    [Fact]
    public void Start_ReportsExaminedFoldersAcrossRoots()
    {
        var rootA = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(rootA, "repo", ".git"));
        var rootB = this.CreateTempDir();
        this.preferences.WatchedDirectories = [rootA, rootB];

        var service = this.CreateService();
        service.Start();

        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && service.Status.IsWatching));
        Assert.Equal(3, service.Status.ScannedDirectoryCount);
    }

    [Fact]
    public void Stop_ClearsScannedDirectoryCount()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        this.preferences.WatchedDirectories = [root];
        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.ScannedDirectoryCount == 2));

        service.Stop();

        Assert.Equal(0, service.Status.ScannedDirectoryCount);
    }

    [Fact]
    public void Rescan_UpdatesOnlyTheRescannedRootsCoverage()
    {
        var rootA = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(rootA, "repo", ".git"));
        var rootB = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(rootB, "repo", ".git"));
        this.preferences.WatchedDirectories = [rootA, rootB];
        this.preferences.ScanDepth = 2;
        this.preferences.DiscoveryMode = ProjectDiscoveryMode.Git;

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle
                                 && service.Status.ScannedDirectoryCount == 4));

        Directory.CreateDirectory(Path.Combine(rootA, "second", ".git"));

        service.Rescan(rootA);

        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle
                                 && service.Status.ScannedDirectoryCount == 5));
    }

    [Fact]
    public void Reload_RemovedRoot_DropsItsScannedFolders()
    {
        var rootA = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(rootA, "repo", ".git"));
        var rootB = this.CreateTempDir();
        this.preferences.WatchedDirectories = [rootA, rootB];

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.ScannedDirectoryCount == 3));

        this.preferences.WatchedDirectories = [rootA];
        service.Reload();

        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle
                                 && service.Status.ScannedDirectoryCount == 2));
    }

    [Fact]
    public void WatcherChange_ArmsDebounceAndScansOnElapsed()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        this.preferences.WatchedDirectories = [root];
        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => this.watcherFactory.Watchers.Count == 1 && this.timerFactory.Timers.Count == 1));
        var initialCount = this.reconciliations.Count;

        this.watcherFactory.Watchers[0].RaiseChanged();
        Assert.NotNull(this.timerFactory.Timers[0].Armed);

        this.timerFactory.Timers[0].Fire();

        Assert.True(WaitFor(() => this.reconciliations.Count > initialCount));
    }

    [Fact]
    public void Reload_RemovedRoot_RemovesItsDiscoveredProject()
    {
        var root = this.CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        this.projects.Add(new Project { Id = "p", Name = "repo", Path = repo, Source = ProjectSource.Discovered });
        this.preferences.WatchedDirectories = [root];

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => this.reconciliations.Count > 0));
        Assert.DoesNotContain("p", this.reconciliations[^1].Removed);

        this.preferences.WatchedDirectories = null;
        service.Reload();

        Assert.True(WaitFor(() => this.reconciliations.Any(r => r.Removed.Contains("p"))));
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.NotConfigured));
    }

    [Fact]
    public void StatusChanged_IsRaisedOnStart()
    {
        var service = this.CreateService();
        var statuses = new List<ProjectScanStatus>();
        service.StatusChanged += (_, status) => statuses.Add(status);

        service.Start();

        Assert.True(WaitFor(() => statuses.Count > 0));
    }

    [Fact]
    public void Rescan_ReconcilesOnlyTheRequestedRoot()
    {
        var rootA = this.CreateTempDir();
        var rootB = this.CreateTempDir();
        this.preferences.WatchedDirectories = [rootA, rootB];
        this.preferences.ScanDepth = 2;
        this.preferences.DiscoveryMode = ProjectDiscoveryMode.Git;

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && this.reconciliations.Count > 0));

        var discoveredRepo = Path.Combine(rootA, "newrepo");
        Directory.CreateDirectory(Path.Combine(discoveredRepo, ".git"));

        this.projects.Add(new Project
        {
            Id = "vanished-a",
            Name = "vanished-a",
            Path = Path.Combine(rootA, "vanished-a"),
            Source = ProjectSource.Discovered
        });
        this.projects.Add(new Project
        {
            Id = "vanished-b",
            Name = "vanished-b",
            Path = Path.Combine(rootB, "vanished-b"),
            Source = ProjectSource.Discovered
        });
        this.projects.Add(new Project
        {
            Id = "manual-a",
            Name = "manual-a",
            Path = Path.Combine(rootA, "manual-a"),
            Source = ProjectSource.Manual
        });

        service.Rescan(rootA);

        Assert.True(WaitFor(() => this.reconciliations.Any(r =>
            r.Discovered.Any(path => string.Equals(path, discoveredRepo, StringComparison.OrdinalIgnoreCase)))));

        var last = this.reconciliations[^1];
        Assert.Contains(discoveredRepo, last.Discovered);
        Assert.Contains("vanished-a", last.Removed);
        Assert.DoesNotContain("vanished-b", last.Removed);
        Assert.DoesNotContain("manual-a", last.Removed);
    }

    [Fact]
    public void Rescan_MissingRoot_RemovesNothing()
    {
        var root = this.CreateTempDir();
        var missingRoot = Path.Combine(root, "missing");
        this.preferences.WatchedDirectories = [root];

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && this.reconciliations.Count > 0));

        this.projects.Add(new Project
        {
            Id = "p",
            Name = "missing",
            Path = Path.Combine(missingRoot, "repo"),
            Source = ProjectSource.Discovered
        });
        var scanCount = this.reconciliations.Count;

        service.Rescan(missingRoot);

        Assert.True(WaitFor(() => this.reconciliations.Count > scanCount));
        Assert.DoesNotContain("p", this.reconciliations[^1].Removed);
    }

    [Fact]
    public void Rescan_KeepsProjectUnderAnotherWatchedRoot()
    {
        var root = this.CreateTempDir();
        var nestedRoot = Path.Combine(root, "nested");
        Directory.CreateDirectory(nestedRoot);
        this.preferences.WatchedDirectories = [root, nestedRoot];

        var service = this.CreateService();
        service.Start();
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && this.reconciliations.Count > 0));

        this.projects.Add(new Project
        {
            Id = "nested-project",
            Name = "vanished",
            Path = Path.Combine(nestedRoot, "vanished"),
            Source = ProjectSource.Discovered
        });
        var scanCount = this.reconciliations.Count;

        service.Rescan(root);

        Assert.True(WaitFor(() => this.reconciliations.Count > scanCount));
        Assert.DoesNotContain("nested-project", this.reconciliations[^1].Removed);
    }

    [Fact]
    public void CountManualProjects_CountsOnlyManualProjects()
    {
        this.projects.Add(new Project
        {
            Id = "manual-a",
            Name = "manual-a",
            Path = Path.Combine(this.CreateTempDir(), "manual-a"),
            Source = ProjectSource.Manual
        });
        this.projects.Add(new Project
        {
            Id = "manual-b",
            Name = "manual-b",
            Path = Path.Combine(this.CreateTempDir(), "manual-b"),
            Source = ProjectSource.Manual
        });
        this.projects.Add(new Project
        {
            Id = "discovered",
            Name = "discovered",
            Path = Path.Combine(this.CreateTempDir(), "discovered"),
            Source = ProjectSource.Discovered
        });

        var service = this.CreateService();

        Assert.Equal(2, service.CountManualProjects());
    }

    [Fact]
    public void CountDiscoveredProjects_CountsOnlyDiscoveredProjectsAtOrBelowRoot()
    {
        var root = this.CreateTempDir();
        var otherRoot = this.CreateTempDir();
        this.projects.Add(new Project
        {
            Id = "a",
            Name = "a",
            Path = Path.Combine(root, "repo"),
            Source = ProjectSource.Discovered
        });
        this.projects.Add(new Project
        {
            Id = "b",
            Name = "b",
            Path = Path.Combine(root, "nested", "repo"),
            Source = ProjectSource.Discovered
        });
        this.projects.Add(new Project
        {
            Id = "c",
            Name = "c",
            Path = Path.Combine(root, "manual"),
            Source = ProjectSource.Manual
        });
        this.projects.Add(new Project
        {
            Id = "d",
            Name = "d",
            Path = Path.Combine(otherRoot, "repo"),
            Source = ProjectSource.Discovered
        });

        var service = this.CreateService();

        Assert.Equal(2, service.CountDiscoveredProjects(root));
        Assert.Equal(0, service.CountDiscoveredProjects(string.Empty));
    }
}
