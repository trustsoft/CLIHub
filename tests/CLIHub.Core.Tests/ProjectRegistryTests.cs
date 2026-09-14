using CLIHub.Core.Abstractions;
using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class ProjectRegistryTests
{
    private static (ProjectRegistry Registry, ConfigStore Store) Create(string configDirectory)
    {
        var fileSystem = new PhysicalFileSystem();
        var store = new ConfigStore(fileSystem, new StubPathProvider(configDirectory, configDirectory));
        return (new ProjectRegistry(store, fileSystem), store);
    }

    [Fact]
    public void Add_ValidFolder_AddsAndPersists()
    {
        using var temp = new TempDirectory();
        var projectDirectory = Path.Combine(temp.Path, "demo");
        Directory.CreateDirectory(projectDirectory);
        var (registry, store) = Create(temp.Path);

        var result = registry.Add(projectDirectory);

        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal("demo", result.Project!.Name);
        Assert.False(string.IsNullOrWhiteSpace(result.Project.Id));
        Assert.Single(store.Load().Projects);
    }

    [Fact]
    public void Add_NonexistentPath_RejectedAndNotPersisted()
    {
        using var temp = new TempDirectory();
        var (registry, store) = Create(temp.Path);

        var result = registry.Add(Path.Combine(temp.Path, "missing"));

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(store.Load().Projects);
    }

    [Fact]
    public void Add_DuplicatePath_Rejected()
    {
        using var temp = new TempDirectory();
        var projectDirectory = Path.Combine(temp.Path, "demo");
        Directory.CreateDirectory(projectDirectory);
        var (registry, _) = Create(temp.Path);
        registry.Add(projectDirectory);

        var result = registry.Add(projectDirectory);

        Assert.False(result.Success);
        Assert.Single(registry.Projects);
    }

    [Fact]
    public void Add_TrailingSeparatorSameFolder_TreatedAsDuplicate()
    {
        using var temp = new TempDirectory();
        var projectDirectory = Path.Combine(temp.Path, "demo");
        Directory.CreateDirectory(projectDirectory);
        var (registry, _) = Create(temp.Path);
        registry.Add(projectDirectory);

        var result = registry.Add(projectDirectory + Path.DirectorySeparatorChar);

        Assert.False(result.Success);
    }

    [Fact]
    public void Remove_ExistingProject_RemovesAndLeavesFolderOnDisk()
    {
        using var temp = new TempDirectory();
        var projectDirectory = Path.Combine(temp.Path, "demo");
        Directory.CreateDirectory(projectDirectory);
        var (registry, store) = Create(temp.Path);
        var added = registry.Add(projectDirectory).Project!;

        var removed = registry.Remove(added.Id!);

        Assert.True(removed);
        Assert.Empty(registry.Projects);
        Assert.Empty(store.Load().Projects);
        Assert.True(Directory.Exists(projectDirectory));
    }

    [Fact]
    public void Load_ExposesPersistedProjects()
    {
        using var temp = new TempDirectory();
        var projectDirectory = Path.Combine(temp.Path, "demo");
        Directory.CreateDirectory(projectDirectory);
        var (registry, _) = Create(temp.Path);
        registry.Add(projectDirectory);

        var (reloaded, _) = Create(temp.Path);

        var project = Assert.Single(reloaded.Projects);
        Assert.Equal("demo", project.Name);
        Assert.False(reloaded.Remove("nonexistent"));
    }
}
