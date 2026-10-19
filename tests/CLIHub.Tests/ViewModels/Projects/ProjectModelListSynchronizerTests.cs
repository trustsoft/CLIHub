namespace CLIHub.ViewModels;

public class ProjectModelListSynchronizerTests
{
    [Fact]
    public void Synchronize_PreservesRowInstancesAcrossRefreshes()
    {
        var rows = new System.Collections.ObjectModel.ObservableCollection<ProjectModel>();
        var models = new[] { Model("a"), Model("b") };
        ProjectModelListSynchronizer.Synchronize(rows, models);

        ProjectModelListSynchronizer.Synchronize(rows, models);

        Assert.Same(models[0], rows[0]);
        Assert.Same(models[1], rows[1]);
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public void Synchronize_ReordersWithoutRecreatingRows()
    {
        var rows = new System.Collections.ObjectModel.ObservableCollection<ProjectModel>();
        var models = new[] { Model("a"), Model("b"), Model("c") };
        ProjectModelListSynchronizer.Synchronize(rows, models);
        var raised = new List<string?>();
        rows.CollectionChanged += (_, e) => raised.Add(e.Action.ToString());

        ProjectModelListSynchronizer.Synchronize(rows, new[] { models[2], models[0], models[1] });

        Assert.Equal(new[] { models[2], models[0], models[1] }, rows.ToArray());
        Assert.DoesNotContain("Add", raised);
        Assert.DoesNotContain("Remove", raised);
    }

    [Fact]
    public void Synchronize_RemovesVanished_AndInsertsNewRows()
    {
        var rows = new System.Collections.ObjectModel.ObservableCollection<ProjectModel>();
        var kept = Model("kept");
        ProjectModelListSynchronizer.Synchronize(rows, new[] { kept, Model("gone") });

        ProjectModelListSynchronizer.Synchronize(rows, new[] { Model("new"), kept });

        Assert.Equal(2, rows.Count);
        Assert.Same(kept, rows[1]);
        Assert.Equal("new", rows[0].Id);
    }

    private static ProjectModel Model(string id) =>
        new(new CLIHub.Core.Projects.EnrichedProject(
            new CLIHub.Core.Models.Project { Id = id, Name = id, Path = $"C:\\{id}" },
            FolderExists: false,
            LogoPath: null));
}
