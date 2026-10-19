namespace CLIHub.ViewModels;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

public class ProjectModelCatalogTests
{
    [Fact]
    public void Sync_NewSnapshots_CreatesRowsInSnapshotOrder()
    {
        var catalog = new ProjectModelCatalog();

        var rows = catalog.Sync([Snapshot("b", "B"), Snapshot("a", "A")]);

        Assert.Equal(new[] { "b", "a" }, rows.Select(row => row.Id));
        Assert.Equal(2, catalog.Models.Count);
    }

    [Fact]
    public void Sync_RepeatedSnapshots_ReturnsSameInstances()
    {
        var catalog = new ProjectModelCatalog();
        var first = catalog.Sync([Snapshot("a", "A")]);

        var second = catalog.Sync([Snapshot("a", "A")]);

        Assert.Same(first.Single(), second.Single());
        Assert.Single(catalog.Models);
    }

    [Fact]
    public void Sync_ChangedSnapshot_UpdatesRowInPlaceWithNotification()
    {
        var catalog = new ProjectModelCatalog();
        var row = catalog.Sync([Snapshot("a", "A", isFavorite: false)]).Single();
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        catalog.Sync([Snapshot("a", "A renamed", isFavorite: true)]);

        Assert.Equal("A renamed", row.Name);
        Assert.True(row.IsFavorite);
        Assert.Contains(nameof(ProjectModel.Name), raised);
        Assert.Contains(nameof(ProjectModel.IsFavorite), raised);
    }

    [Fact]
    public void Sync_VanishedId_DropsRowFromCatalog()
    {
        var catalog = new ProjectModelCatalog();
        catalog.Sync([Snapshot("a", "A"), Snapshot("gone", "Gone")]);

        var rows = catalog.Sync([Snapshot("a", "A")]);

        Assert.Equal(new[] { "a" }, rows.Select(row => row.Id));
        Assert.Single(catalog.Models);
    }

    [Fact]
    public void Sync_EmptySnapshots_DropsAllRows()
    {
        var catalog = new ProjectModelCatalog();
        catalog.Sync([Snapshot("a", "A")]);

        Assert.Empty(catalog.Sync([]));
        Assert.Empty(catalog.Models);
    }

    [Fact]
    public void Sync_NullSnapshot_Throws()
    {
        var catalog = new ProjectModelCatalog();

        Assert.Throws<ArgumentNullException>(() => catalog.Sync([null!]));
    }

    [Fact]
    public void Model_CarriesSnapshotValuesAndDerivedFields()
    {
        var row = new ProjectModel(Snapshot(
            "manual",
            "Manual",
            path: AppContext.BaseDirectory,
            isFavorite: true,
            logoPath: "logo.png",
            source: ProjectSource.Manual,
            folderExists: true));

        Assert.Equal("manual", row.Id);
        Assert.Equal("Manual", row.Name);
        Assert.Equal(AppContext.BaseDirectory, row.Path);
        Assert.Equal("logo.png", row.LogoPath);
        Assert.True(row.IsFolderAvailable);
        Assert.True(row.IsFavorite);
        Assert.True(row.IsManual);
        Assert.True(row.CanRemove);
        Assert.True(row.CanOpen);
        Assert.Equal("M", row.SourceMark);
        Assert.Equal("Manual", row.SourceLabel);
        Assert.Equal(AppContext.BaseDirectory, row.DisplayPath);
    }

    [Fact]
    public void Model_DiscoveredProject_IsReadOnlyForRemoval()
    {
        var row = new ProjectModel(Snapshot("discovered", "Discovered", source: ProjectSource.Discovered));

        Assert.False(row.IsManual);
        Assert.False(row.CanRemove);
        Assert.Equal("D", row.SourceMark);
        Assert.Equal("Discovered", row.SourceLabel);
    }

    [Fact]
    public void Model_FolderAvailabilityChange_RaisesCanOpen()
    {
        var row = new ProjectModel(Snapshot("a", "A", folderExists: true));
        var raised = new List<string?>();
        row.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        row.Update(Snapshot("a", "A", folderExists: false));

        Assert.False(row.CanOpen);
        Assert.Contains(nameof(ProjectModel.IsFolderAvailable), raised);
        Assert.Contains(nameof(ProjectModel.CanOpen), raised);
    }

    [Fact]
    public void Update_UnchangedValues_RaisesNothing()
    {
        var row = new ProjectModel(Snapshot("a", "A"));
        var raised = 0;
        row.PropertyChanged += (_, _) => raised++;

        row.Update(Snapshot("a", "A"));

        Assert.Equal(0, raised);
    }

    [Fact]
    public void Update_DifferentProjectId_Throws()
    {
        var row = new ProjectModel(Snapshot("a", "A"));

        Assert.Throws<ArgumentException>(() => row.Update(Snapshot("b", "B")));
    }

    private static EnrichedProject Snapshot(
        string id,
        string name,
        string? path = null,
        bool isFavorite = false,
        string? logoPath = null,
        ProjectSource source = ProjectSource.Manual,
        bool folderExists = false) =>
        new(
            new Project
            {
                Id = id,
                Name = name,
                Path = path ?? $"C:\\{id}",
                Source = source,
                IsFavorite = isFavorite
            },
            folderExists,
            logoPath);
}
