namespace CLIHub.Core.Tests.Scanning;

using CLIHub.Core.Models;
using CLIHub.Core.Scanning;

public class RepositoryDiscoveryTests : IDisposable
{
    private readonly List<string> tempDirs = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-scan-" + Guid.NewGuid().ToString("N"));
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

    private static ProjectScanningOptions Options(
        string root,
        int depth = 3,
        ProjectDiscoveryMode mode = ProjectDiscoveryMode.Git,
        IReadOnlyList<string>? ignored = null) =>
        new([root], depth, ignored ?? ProjectScanningDefaults.IgnoredPatterns, mode);

    private static Plugin AgentPlugin(params string[] indicators) => new()
    {
        Id = "test-agent",
        Name = "Test Agent",
        Detection = new AgentDetection { ProjectIndicators = indicators.ToList() }
    };

    [Fact]
    public void Enumerate_GitDirectory_IsDiscovered()
    {
        var root = this.CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));

        var result = RepositoryDiscovery.Enumerate(Options(root), []);

        Assert.Equal([repo], result.DiscoveredPaths);
        Assert.Equal([root], result.ScannedRoots);
    }

    [Fact]
    public void Enumerate_RespectsScanDepth()
    {
        var root = this.CreateTempDir();
        var repo = Path.Combine(root, "a", "b");
        Directory.CreateDirectory(Path.Combine(repo, ".git"));

        Assert.Empty(RepositoryDiscovery.Enumerate(Options(root, depth: 1), []).DiscoveredPaths);
        Assert.Equal([repo], RepositoryDiscovery.Enumerate(Options(root, depth: 2), []).DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_SkipsIgnoredPatterns()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "node_modules", "pkg", ".git"));

        var result = RepositoryDiscovery.Enumerate(Options(root), []);

        Assert.Empty(result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_DoesNotDescendIntoGitDirectory()
    {
        var root = this.CreateTempDir();
        var repo = Path.Combine(root, "repo");
        Directory.CreateDirectory(Path.Combine(repo, ".git", "nested", ".git"));

        var result = RepositoryDiscovery.Enumerate(Options(root), []);

        Assert.Equal([repo], result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_GitMode_IgnoresAgentOnlyFolder()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "agentonly", ".claude"));

        var result = RepositoryDiscovery.Enumerate(Options(root, mode: ProjectDiscoveryMode.Git), [AgentPlugin(".claude")]);

        Assert.Empty(result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_AgentsMode_IgnoresPlainGitRepository()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "repo", ".git"));

        var result = RepositoryDiscovery.Enumerate(Options(root, mode: ProjectDiscoveryMode.Agents), [AgentPlugin(".claude")]);

        Assert.Empty(result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_BothMode_DiscoveriesEitherCondition()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "gitrepo", ".git"));
        var agent = Path.Combine(root, "agentonly");
        Directory.CreateDirectory(Path.Combine(agent, ".claude"));

        var result = RepositoryDiscovery.Enumerate(Options(root, mode: ProjectDiscoveryMode.Both), [AgentPlugin(".claude")]);

        Assert.Equal(2, result.DiscoveredPaths.Count);
        Assert.Contains(Path.Combine(root, "gitrepo"), result.DiscoveredPaths);
        Assert.Contains(agent, result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_OpenSpecFolder_IsNotAnAgentMarker()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, "spec-only", "openspec"));

        var result = RepositoryDiscovery.Enumerate(
            Options(root, mode: ProjectDiscoveryMode.Agents),
            [AgentPlugin(".opencode", "openspec")]);

        Assert.Empty(result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_MissingRoot_IsSkippedWithWarning()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var options = new ProjectScanningOptions([missing], 2, [], ProjectDiscoveryMode.Git);
        var errors = new List<string>();

        var result = RepositoryDiscovery.Enumerate(options, [], (root, _) => errors.Add(root));

        Assert.Empty(result.DiscoveredPaths);
        Assert.Empty(result.ScannedRoots);
        Assert.Equal([missing], errors);
    }

    [Fact]
    public void Enumerate_RootItselfIsRepository_IsDiscovered()
    {
        var root = this.CreateTempDir();
        Directory.CreateDirectory(Path.Combine(root, ".git"));

        var result = RepositoryDiscovery.Enumerate(Options(root), []);

        Assert.Equal([root], result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_UsesPerRootScanDepth()
    {
        var rootA = this.CreateTempDir();
        var rootB = this.CreateTempDir();
        var deepRepo = Path.Combine(rootA, "a", "b");
        Directory.CreateDirectory(Path.Combine(deepRepo, ".git"));
        var shallowRepo = Path.Combine(rootB, "b");
        Directory.CreateDirectory(Path.Combine(shallowRepo, ".git"));
        var options = new ProjectScanningOptions(
            [rootA, rootB],
            1,
            ProjectScanningDefaults.IgnoredPatterns,
            ProjectDiscoveryMode.Git,
            new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [rootA] = new(2, ProjectDiscoveryMode.Git)
            });

        var result = RepositoryDiscovery.Enumerate(options, []);

        Assert.Contains(deepRepo, result.DiscoveredPaths);
        Assert.Contains(shallowRepo, result.DiscoveredPaths);
    }

    [Fact]
    public void Enumerate_UsesPerRootDiscoveryMode()
    {
        var root = this.CreateTempDir();
        var agentRepo = Path.Combine(root, "agent");
        Directory.CreateDirectory(Path.Combine(agentRepo, ".claude"));
        var options = new ProjectScanningOptions(
            [root],
            3,
            ProjectScanningDefaults.IgnoredPatterns,
            ProjectDiscoveryMode.Git,
            new Dictionary<string, WatchedDirectoryScanSettings>
            {
                [root] = new(3, ProjectDiscoveryMode.Agents)
            });

        var result = RepositoryDiscovery.Enumerate(options, [AgentPlugin(".claude")]);

        Assert.Contains(agentRepo, result.DiscoveredPaths);
    }
}
