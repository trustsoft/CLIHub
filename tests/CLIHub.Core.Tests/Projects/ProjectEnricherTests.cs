namespace CLIHub.Core.Tests.Projects;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;

public class ProjectEnricherTests : IDisposable
{
    private readonly List<string> tempDirs = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-tests-" + Guid.NewGuid().ToString("N"));
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

    private ProjectEnricher CreateEnricher(string? defaultLogoPath = null, LogoCacheService? cache = null)
    {
        cache ??= new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            Path.Combine(this.CreateTempDir(), "logos.json"));

        return new ProjectEnricher(new ProjectLogoResolver(cache, new LogoOptions(defaultLogoPath)));
    }

    private static Project Project(string id, string path) => new()
    {
        Id = id,
        Name = id,
        Path = path
    };

    [Fact]
    public void Enrich_ExistingFolder_MarksFolderAvailable()
    {
        var folder = this.CreateTempDir();
        var enricher = this.CreateEnricher();

        var snapshot = enricher.Enrich([Project("p", folder)]).Single();

        Assert.True(snapshot.FolderExists);
    }

    [Fact]
    public void Enrich_MissingFolder_MarksFolderUnavailable()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var enricher = this.CreateEnricher();

        var snapshot = enricher.Enrich([Project("p", missing)]).Single();

        Assert.False(snapshot.FolderExists);
    }

    [Fact]
    public void Enrich_FolderLogoFound_ReturnsResolvedLogoPath()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var enricher = this.CreateEnricher();

        var snapshot = enricher.Enrich([Project("p", folder)]).Single();

        Assert.Equal(Path.Combine(folder, "logo.png"), snapshot.LogoPath);
    }

    [Fact]
    public void Enrich_NoLogoInFolder_FallsBackToDefaultLogoPath()
    {
        var folder = this.CreateTempDir();
        var enricher = this.CreateEnricher(defaultLogoPath: "default.png");

        var snapshot = enricher.Enrich([Project("p", folder)]).Single();

        Assert.Equal("default.png", snapshot.LogoPath);
    }

    [Fact]
    public void Enrich_SecondCall_ServesLogoFromCacheWithoutFolderRescan()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var enricher = this.CreateEnricher();
        var project = Project("p", folder);
        var first = enricher.Enrich([project]).Single();

        File.Delete(Path.Combine(folder, "logo.png"));
        var second = enricher.Enrich([project]).Single();

        Assert.Equal(first.LogoPath, second.LogoPath);
    }

    [Fact]
    public void Enrich_EmptyList_ReturnsEmpty()
    {
        var enricher = this.CreateEnricher();

        Assert.Empty(enricher.Enrich([]));
    }

    [Fact]
    public void Enrich_PreservesInputOrder()
    {
        var enricher = this.CreateEnricher();

        var snapshots = enricher.Enrich([Project("b", "C:\\b"), Project("a", "C:\\a")]);

        Assert.Equal(new[] { "b", "a" }, snapshots.Select(snapshot => snapshot.Project.Id));
    }
}
