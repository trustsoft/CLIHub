namespace CLIHub.Core.Tests.Projects;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

public class ProjectListOrderingTests
{
    [Fact]
    public void FavoritesFirst_MixedList_FavoritesBeforeNonFavorites()
    {
        var plain = Project("plain");
        var favorite = Project("favorite", isFavorite: true);
        var projects = new List<Project> { plain, favorite, Project("other") };

        var ordered = ProjectListOrdering.FavoritesFirst(projects);

        Assert.Equal(new[] { "favorite", "plain", "other" }, ordered.Select(project => project.Id));
    }

    [Fact]
    public void FavoritesFirst_AllFavorites_PreservesOrder()
    {
        var projects = new List<Project>
        {
            Project("first", isFavorite: true),
            Project("second", isFavorite: true),
            Project("third", isFavorite: true)
        };

        var ordered = ProjectListOrdering.FavoritesFirst(projects);

        Assert.Equal(new[] { "first", "second", "third" }, ordered.Select(project => project.Id));
    }

    [Fact]
    public void FavoritesFirst_NoFavorites_PreservesOrder()
    {
        var projects = new List<Project>
        {
            Project("first"),
            Project("second"),
            Project("third")
        };

        var ordered = ProjectListOrdering.FavoritesFirst(projects);

        Assert.Equal(new[] { "first", "second", "third" }, ordered.Select(project => project.Id));
    }

    [Fact]
    public void FavoritesFirst_EmptyList_ReturnsEmpty()
    {
        var ordered = ProjectListOrdering.FavoritesFirst([]);

        Assert.Empty(ordered);
    }

    [Fact]
    public void FavoritesFirst_SingleFavorite_ReturnsSingle()
    {
        var favorite = Project("favorite", isFavorite: true);

        var ordered = ProjectListOrdering.FavoritesFirst([favorite]);

        var row = Assert.Single(ordered);
        Assert.Same(favorite, row);
    }

    [Fact]
    public void FavoritesFirst_StableWithinGroups_PreservesInsertionOrder()
    {
        var projects = new List<Project>
        {
            Project("fav-a", isFavorite: true),
            Project("plain-a"),
            Project("fav-b", isFavorite: true),
            Project("plain-b"),
            Project("fav-c", isFavorite: true),
            Project("plain-c")
        };

        var ordered = ProjectListOrdering.FavoritesFirst(projects);

        Assert.Equal(
            new[] { "fav-a", "fav-b", "fav-c", "plain-a", "plain-b", "plain-c" },
            ordered.Select(project => project.Id));
    }

    private static Project Project(string id, bool isFavorite = false) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}",
        IsFavorite = isFavorite
    };
}
