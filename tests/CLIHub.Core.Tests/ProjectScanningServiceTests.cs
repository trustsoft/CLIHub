namespace CLIHub.Tests.Scanning;

using System.Diagnostics;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Scanning;
using CLIHub.Tests.Services;

public class ProjectScanningServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = new();
    private readonly FakeRepositoryWatcherFactory _watcherFactory = new();
    private readonly FakeScanTimerFactory _timerFactory = new();
    private readonly AppPreferences _preferences = new();
    private readonly List<Project> _projects = new();
    private readonly List<(IReadOnlyCollection<string> Discovered, IReadOnlyCollection<string> Removed)> _reconciliations = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-scan-svc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        _tempDirs.Add(dir);
        return dir;
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private ProjectScanningService CreateService()
    {
        var preferences = new Mock<IPreferencesStore>();
        preferences.Setup(x => x.Load()).Returns(() => _preferences);

        var projects = new Mock<IProjectService>();
        projects.Setup(x => x.GetAllProjects()).Returns(() => _projects.ToList());
        projects
            .Setup(x => x.ReconcileDiscoveredProjects(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<IReadOnlyCollection<string>>()))
            .Callback<IReadOnlyCollection<string>, IReadOnlyCollection<string>>((d, r) => _reconciliations.Add((d, r)))
            .Returns(false);

        var plugins = new Mock<IPluginCatalog>();
        plugins.Setup(x => x.GetAllPlugins()).Returns([]);

        return new ProjectScanningService(
            preferences.Object,
            projects.Object,
            plugins.Object,
            _watcherFactory,
            _timerFactory,
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
        var service = CreateService();

        service.Start();

        Assert.Equal(ProjectScanState.NotConfigured, service.Status.State);
        Assert.Empty(_watcherFactory.CreatedRoots);
    }

    [Fact]
    public void Start_WithWatchedDirectory_WatchesAndReconcilesDiscoveredRepository()
    {
        var root = CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        _preferences.WatchedDirectories = [root];
        _preferences.ScanDepth = 2;
        _preferences.DiscoveryMode = ProjectDiscoveryMode.Both;

        var service = CreateService();
        service.Start();

        Assert.True(WaitFor(() => _reconciliations.Count > 0));
        Assert.Single(_watcherFactory.Watchers);
        Assert.True(_watcherFactory.Watchers[0].Started);
        Assert.Contains(repo, _reconciliations[^1].Discovered);
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.Idle && service.Status.IsWatching));
    }

    [Fact]
    public void Stop_DisposesWatcherAndClearsStatus()
    {
        var root = CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        _preferences.WatchedDirectories = [root];
        var service = CreateService();
        service.Start();
        Assert.True(WaitFor(() => _watcherFactory.Watchers.Count == 1));

        service.Stop();

        Assert.True(_watcherFactory.Watchers[0].Disposed);
        Assert.Equal(ProjectScanState.NotConfigured, service.Status.State);
    }

    [Fact]
    public void WatcherChange_ArmsDebounceAndScansOnElapsed()
    {
        var root = CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));
        _preferences.WatchedDirectories = [root];
        var service = CreateService();
        service.Start();
        Assert.True(WaitFor(() => _watcherFactory.Watchers.Count == 1 && _timerFactory.Timers.Count == 1));
        var initialCount = _reconciliations.Count;

        _watcherFactory.Watchers[0].RaiseChanged();
        Assert.NotNull(_timerFactory.Timers[0].Armed);

        _timerFactory.Timers[0].Fire();

        Assert.True(WaitFor(() => _reconciliations.Count > initialCount));
    }

    [Fact]
    public void Reload_RemovedRoot_RemovesItsDiscoveredProject()
    {
        var root = CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));
        _projects.Add(new Project { Id = "p", Name = "repo", Path = repo, Source = ProjectSource.Discovered });
        _preferences.WatchedDirectories = [root];

        var service = CreateService();
        service.Start();
        Assert.True(WaitFor(() => _reconciliations.Count > 0));
        Assert.DoesNotContain("p", _reconciliations[^1].Removed);

        _preferences.WatchedDirectories = null;
        service.Reload();

        Assert.True(WaitFor(() => _reconciliations.Any(r => r.Removed.Contains("p"))));
        Assert.True(WaitFor(() => service.Status.State == ProjectScanState.NotConfigured));
    }

    [Fact]
    public void StatusChanged_IsRaisedOnStart()
    {
        var service = CreateService();
        var statuses = new List<ProjectScanStatus>();
        service.StatusChanged += (_, status) => statuses.Add(status);

        service.Start();

        Assert.True(WaitFor(() => statuses.Count > 0));
    }
}
