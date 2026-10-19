namespace CLIHub.Core.Tests.Projects;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Projects;

public class ProjectLogoResolverTests : IDisposable
{
    private readonly List<string> tempDirs = new();

    [Fact]
    public void ResolvePath_ReturnsFirstExistingCandidate()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");

        var resolver = this.CreateResolver();

        Assert.Equal(Path.Combine(folder, "logo.png"), resolver.ResolvePath(folder));
    }

    [Fact]
    public void ResolvePath_ReturnsLaterCandidate_WhenEarlierMissing()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");

        var resolver = this.CreateResolver();

        Assert.Equal(Path.Combine(folder, "icon.png"), resolver.ResolvePath(folder));
    }

    [Fact]
    public void ResolvePath_NoMatch_ReturnsDefault()
    {
        var folder = this.CreateTempDir();
        var resolver = this.CreateResolver(defaultLogoPath: "default.png");

        Assert.Equal("default.png", resolver.ResolvePath(folder));
    }

    [Fact]
    public void Resolve_AppliesDefaultFallback_AtResolverLevel()
    {
        var folder = this.CreateTempDir();
        var resolver = this.CreateResolver(defaultLogoPath: "default.png");

        Assert.Equal("default.png", resolver.Resolve(this.Project(folder)));
    }

    [Fact]
    public void Resolve_SecondCall_ServedFromCacheWithoutRescan()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cache = this.CreateCache();
        var resolver = new ProjectLogoResolver(cache, new LogoOptions(null));
        var project = this.Project(folder);
        var first = resolver.Resolve(project);

        File.Delete(Path.Combine(folder, "logo.png"));

        Assert.Equal(first, resolver.Resolve(project));
    }

    [Fact]
    public void Resolve_AfterResolverRestart_ServedFromPersistentCache()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cachePath = Path.Combine(this.CreateTempDir(), "logos.json");
        var firstCache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath);
        var first = new ProjectLogoResolver(firstCache, new LogoOptions(null));
        var project = this.Project(folder);
        var cached = first.Resolve(project);
        firstCache.Dispose();

        File.Delete(Path.Combine(folder, "logo.png"));
        var second = new ProjectLogoResolver(
            new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath),
            new LogoOptions(null));

        Assert.Equal(cached, second.Resolve(project));
    }

    [Fact]
    public void Remove_EvictsCacheEntry_SoNextResolveRescans()
    {
        var folder = this.CreateTempDir();
        var logoPath = Path.Combine(folder, "logo.png");
        File.WriteAllText(logoPath, "x");
        var cache = this.CreateCache();
        var resolver = new ProjectLogoResolver(cache, new LogoOptions(null));
        var project = this.Project(folder);
        Assert.Equal(logoPath, resolver.Resolve(project));

        File.Delete(logoPath);
        resolver.Remove(project.Id);

        Assert.Null(resolver.Resolve(project));
    }

    public void Dispose()
    {
        foreach (var dir in this.tempDirs)
        {
            try { Directory.Delete(dir, recursive: true); } catch { }
        }
    }

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        this.tempDirs.Add(dir);
        return dir;
    }

    private LogoCacheService CreateCache() =>
        new(NullLogger<LogoCacheService>.Instance, Path.Combine(this.CreateTempDir(), "logos.json"));

    private ProjectLogoResolver CreateResolver(string? defaultLogoPath = null) =>
        new(this.CreateCache(), new LogoOptions(defaultLogoPath));

    private Project Project(string path) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Name = "project",
        Path = path
    };
}
