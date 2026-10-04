namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

public class ProjectServiceTests : IDisposable
{
    private readonly List<string> _tempDirs = new();

    private string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "clihub-tests-" + Guid.NewGuid().ToString("N"));
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

    private ProjectService CreateService(AppConfig? config = null, string? defaultLogoPath = null)
    {
        var cache = new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            Path.Combine(CreateTempDir(), "logos.json"));

        return new ProjectService(
            new FakeConfigService(config),
            cache,
            NullLogger<ProjectService>.Instance)
        {
            DefaultLogoPath = defaultLogoPath
        };
    }

    [Fact]
    public void AddProject_CreatesProject_WithFolderDerivedName()
    {
        var folder = CreateTempDir();
        var service = CreateService();

        var project = service.AddProject(folder);

        Assert.Equal(Path.GetFileName(folder), project.Name);
        Assert.False(string.IsNullOrWhiteSpace(project.Id));
        Assert.Single(service.GetAllProjects());
    }

    [Fact]
    public void AddProject_DuplicatePath_ReturnsExisting()
    {
        var folder = CreateTempDir();
        var service = CreateService();

        var first = service.AddProject(folder);
        var second = service.AddProject(folder + Path.DirectorySeparatorChar);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(service.GetAllProjects());
    }

    [Fact]
    public void AddProject_MissingFolder_Throws()
    {
        var service = CreateService();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => service.AddProject(missing));
    }

    [Fact]
    public void SetCurrentProject_UpdatesCurrentAndRaisesEvent()
    {
        var folder = CreateTempDir();
        var service = CreateService();
        var project = service.AddProject(folder);

        var raised = false;
        service.ProjectsChanged += (_, _) => raised = true;

        service.SetCurrentProject(project.Id);

        Assert.True(raised);
        Assert.Equal(project.Id, service.GetCurrentProject()?.Id);
    }

    [Fact]
    public void Constructor_StaleCurrentProjectId_IsCleared()
    {
        var config = new AppConfig
        {
            CurrentProjectId = "does-not-exist",
            Projects = { new Project { Id = "real", Name = "Real", Path = "C:\\real" } }
        };

        var service = CreateService(config);

        Assert.Null(service.GetCurrentProject());
    }

    [Fact]
    public void GetRecentProjects_OrdersByLastUsedDescending_AndRespectsLimit()
    {
        var config = new AppConfig
        {
            Projects =
            {
                new Project { Id = "a", Name = "A", Path = "C:\\a", LastUsed = new DateTime(2026, 1, 1) },
                new Project { Id = "b", Name = "B", Path = "C:\\b", LastUsed = new DateTime(2026, 3, 1) },
                new Project { Id = "c", Name = "C", Path = "C:\\c", LastUsed = new DateTime(2026, 2, 1) }
            }
        };

        var service = CreateService(config);

        var recent = service.GetRecentProjects(2);

        Assert.Equal(new[] { "b", "c" }, recent.Select(p => p.Id));
    }

    [Fact]
    public void ToggleFavorite_TogglesFlag()
    {
        var folder = CreateTempDir();
        var service = CreateService();
        var project = service.AddProject(folder);

        service.ToggleFavorite(project.Id);
        Assert.Contains(service.GetFavorites(), p => p.Id == project.Id);

        service.ToggleFavorite(project.Id);
        Assert.DoesNotContain(service.GetFavorites(), p => p.Id == project.Id);
    }

    [Fact]
    public void RemoveProject_ClearsCurrent_AndKeepsFolderOnDisk()
    {
        var folder = CreateTempDir();
        var service = CreateService();
        var project = service.AddProject(folder);
        service.SetCurrentProject(project.Id);

        service.RemoveProject(project.Id);

        Assert.Empty(service.GetAllProjects());
        Assert.Null(service.GetCurrentProject());
        Assert.True(Directory.Exists(folder), "RemoveProject must not delete files on disk");
    }

    [Fact]
    public void ResolveLogo_ReturnsFirstExistingCandidate()
    {
        var folder = CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");

        var service = CreateService();

        var logo = service.ResolveLogo(folder);

        Assert.Equal(Path.Combine(folder, "logo.png"), logo);
    }

    [Fact]
    public void ResolveLogo_ReturnsLaterCandidate_WhenEarlierMissing()
    {
        var folder = CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");

        var service = CreateService();

        var logo = service.ResolveLogo(folder);

        Assert.Equal(Path.Combine(folder, "icon.png"), logo);
    }

    [Fact]
    public void ResolveLogo_NoMatch_ReturnsDefault()
    {
        var folder = CreateTempDir();
        var service = CreateService(defaultLogoPath: "default.png");

        var logo = service.ResolveLogo(folder);

        Assert.Equal("default.png", logo);
    }

    [Fact]
    public void RemoveProject_RemovesLogoCacheEntry()
    {
        var folder = CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(CreateTempDir(), "logos.json"));
        var service = new ProjectService(new FakeConfigService(), cache, NullLogger<ProjectService>.Instance);
        var project = service.AddProject(folder);

        service.RemoveProject(project.Id);
        var calls = 0;
        cache.GetOrResolve($"project:{project.Id}", () => { calls++; return "fresh.png"; });

        Assert.Equal(1, calls);
        Assert.Equal("fresh.png", cache.GetOrResolve($"project:{project.Id}", () => throw new InvalidOperationException("must stay cached")));
    }

    [Fact]
    public void GetAllProjects_LogoAlreadyCached_DoesNotRescan()
    {
        var folder = CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var service = CreateService();
        var project = service.AddProject(folder);
        var cachedLogo = service.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        File.Delete(Path.Combine(folder, "logo.png"));
        var logoAfterDelete = service.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        Assert.Equal(cachedLogo, logoAfterDelete);
    }

    [Fact]
    public void GetAllProjects_AfterServiceRestart_ReuseCachedLogoWithoutRescan()
    {
        var folder = CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cachePath = Path.Combine(CreateTempDir(), "logos.json");
        string cachedLogo;

        var config = new FakeConfigService();
        var firstCache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath);
        var first = new ProjectService(config, firstCache, NullLogger<ProjectService>.Instance);
        var project = first.AddProject(folder);
        cachedLogo = first.GetAllProjects().Single(p => p.Id == project.Id).LogoPath!;
        firstCache.Dispose();

        File.Delete(Path.Combine(folder, "logo.png"));
        var second = new ProjectService(config, new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath), NullLogger<ProjectService>.Instance);

        var logoAfterRestart = second.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        Assert.Equal(cachedLogo, logoAfterRestart);
    }
}
