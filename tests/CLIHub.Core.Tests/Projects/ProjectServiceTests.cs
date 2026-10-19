namespace CLIHub.Core.Tests.Projects;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Projects;
using CLIHub.Core.Tests.Fakes;

public class ProjectServiceTests : IDisposable
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

    private ProjectService CreateService(AppConfig? config = null, string? defaultLogoPath = null)
    {
        var cache = new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            Path.Combine(this.CreateTempDir(), "logos.json"));
        var resolver = new ProjectLogoResolver(cache, new LogoOptions(defaultLogoPath));

        return new ProjectService(
            new ProjectStateStore(new FakeConfigurationRepository(config)),
            resolver,
            NullLogger<ProjectService>.Instance);
    }

    [Fact]
    public void AddProject_CreatesProject_WithFolderDerivedName()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();

        var project = service.AddProject(folder);

        Assert.Equal(Path.GetFileName(folder), project.Name);
        Assert.False(string.IsNullOrWhiteSpace(project.Id));
        Assert.Single(service.GetAllProjects());
    }

    [Fact]
    public void AddProject_DuplicatePath_ReturnsExisting()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();

        var first = service.AddProject(folder);
        var second = service.AddProject(folder + Path.DirectorySeparatorChar);

        Assert.Equal(first.Id, second.Id);
        Assert.Single(service.GetAllProjects());
    }

    [Fact]
    public void AddProject_MissingFolder_Throws()
    {
        var service = this.CreateService();
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

        Assert.Throws<DirectoryNotFoundException>(() => service.AddProject(missing));
    }

    [Fact]
    public void SetCurrentProject_UpdatesCurrentAndRaisesEvent()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();
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

        var service = this.CreateService(config);

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

        var service = this.CreateService(config);

        var recent = service.GetRecentProjects(2);

        Assert.Equal(new[] { "b", "c" }, recent.Select(p => p.Id));
    }

    [Fact]
    public void ToggleFavorite_TogglesFlag()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();
        var project = service.AddProject(folder);

        service.ToggleFavorite(project.Id);
        Assert.True(service.GetAllProjects().Single(p => p.Id == project.Id).IsFavorite);

        service.ToggleFavorite(project.Id);
        Assert.False(service.GetAllProjects().Single(p => p.Id == project.Id).IsFavorite);
    }

    [Fact]
    public void RemoveProject_ClearsCurrent_AndKeepsFolderOnDisk()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();
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
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");

        var service = this.CreateService();

        var logo = service.ResolveLogo(folder);

        Assert.Equal(Path.Combine(folder, "logo.png"), logo);
    }

    [Fact]
    public void ResolveLogo_ReturnsLaterCandidate_WhenEarlierMissing()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "icon.png"), "x");

        var service = this.CreateService();

        var logo = service.ResolveLogo(folder);

        Assert.Equal(Path.Combine(folder, "icon.png"), logo);
    }

    [Fact]
    public void ResolveLogo_NoMatch_ReturnsDefault()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService(defaultLogoPath: "default.png");

        var logo = service.ResolveLogo(folder);

        Assert.Equal("default.png", logo);
    }

    [Fact]
    public void RemoveProject_RemovesLogoCacheEntry()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, Path.Combine(this.CreateTempDir(), "logos.json"));
        var service = new ProjectService(
            new ProjectStateStore(new FakeConfigurationRepository()),
            new ProjectLogoResolver(cache, new LogoOptions(null)),
            NullLogger<ProjectService>.Instance);
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
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var service = this.CreateService();
        var project = service.AddProject(folder);
        var cachedLogo = service.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        File.Delete(Path.Combine(folder, "logo.png"));
        var logoAfterDelete = service.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        Assert.Equal(cachedLogo, logoAfterDelete);
    }

    [Fact]
    public void GetAllProjects_AfterServiceRestart_ReuseCachedLogoWithoutRescan()
    {
        var folder = this.CreateTempDir();
        File.WriteAllText(Path.Combine(folder, "logo.png"), "x");
        var cachePath = Path.Combine(this.CreateTempDir(), "logos.json");
        string cachedLogo;

        var config = new FakeConfigurationRepository();
        var firstCache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath);
        var first = new ProjectService(
            new ProjectStateStore(config),
            new ProjectLogoResolver(firstCache, new LogoOptions(null)),
            NullLogger<ProjectService>.Instance);
        var project = first.AddProject(folder);
        cachedLogo = first.GetAllProjects().Single(p => p.Id == project.Id).LogoPath!;
        firstCache.Dispose();

        File.Delete(Path.Combine(folder, "logo.png"));
        var second = new ProjectService(
            new ProjectStateStore(config),
            new ProjectLogoResolver(
                new LogoCacheService(NullLogger<LogoCacheService>.Instance, cachePath),
                new LogoOptions(null)),
            NullLogger<ProjectService>.Instance);

        var logoAfterRestart = second.GetAllProjects().Single(p => p.Id == project.Id).LogoPath;

        Assert.Equal(cachedLogo, logoAfterRestart);
    }

    [Fact]
    public void AddProject_MarksManualSource()
    {
        var service = this.CreateService();

        var project = service.AddProject(this.CreateTempDir());

        Assert.Equal(ProjectSource.Manual, project.Source);
    }

    [Fact]
    public void AddDiscoveredProject_MarksDiscoveredSource()
    {
        var service = this.CreateService();

        var project = service.AddDiscoveredProject(this.CreateTempDir());

        Assert.Equal(ProjectSource.Discovered, project.Source);
    }

    [Fact]
    public void AddDiscoveredProject_ExistingManual_DoesNotReclassify()
    {
        var folder = this.CreateTempDir();
        var service = this.CreateService();
        var manual = service.AddProject(folder);

        var discovered = service.AddDiscoveredProject(folder);

        Assert.Equal(manual.Id, discovered.Id);
        Assert.Equal(ProjectSource.Manual, discovered.Source);
    }

    [Fact]
    public void ReconcileDiscoveredProjects_AddsDiscoveredAndNeverRemovesManual()
    {
        var manualFolder = this.CreateTempDir();
        var discoveredFolder = this.CreateTempDir();
        var config = new AppConfig
        {
            Projects = { new Project { Id = "m", Name = "m", Path = manualFolder, Source = ProjectSource.Manual } }
        };
        var service = this.CreateService(config);

        var changed = service.ReconcileDiscoveredProjects([discoveredFolder, manualFolder], ["m"]);

        Assert.True(changed);
        var projects = service.GetAllProjects();
        Assert.Equal(2, projects.Count);
        Assert.Equal(ProjectSource.Manual, projects.Single(p => p.Path == manualFolder).Source);
        Assert.Equal(ProjectSource.Discovered, projects.Single(p => p.Path == discoveredFolder).Source);
    }

    [Fact]
    public void ReconcileDiscoveredProjects_RemovesListedDiscoveredProject()
    {
        var folder = this.CreateTempDir();
        var config = new AppConfig
        {
            Projects = { new Project { Id = "d", Name = "d", Path = folder, Source = ProjectSource.Discovered } }
        };
        var service = this.CreateService(config);

        var changed = service.ReconcileDiscoveredProjects([], ["d"]);

        Assert.True(changed);
        Assert.Empty(service.GetAllProjects());
    }

    [Fact]
    public void ReconcileDiscoveredProjects_NoChanges_ReturnsFalse()
    {
        var folder = this.CreateTempDir();
        var config = new AppConfig
        {
            Projects = { new Project { Id = "m", Name = "m", Path = folder, Source = ProjectSource.Manual } }
        };
        var service = this.CreateService(config);

        var changed = service.ReconcileDiscoveredProjects([folder], []);

        Assert.False(changed);
    }
}
