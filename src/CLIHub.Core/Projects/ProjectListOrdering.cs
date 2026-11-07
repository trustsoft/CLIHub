namespace CLIHub.Core.Projects;

using CLIHub.Core.Models;

/// <summary>
///   Presentation-order policy for project lists: favorites first, then the rest, preserving
///   the source order within each group. Pure function with no dependencies; the caller owns
///   the input order and decides when to apply the policy.
/// </summary>
public static class ProjectListOrdering
{
    /// <summary>
    ///   Partitions projects into favorites first, non-favorites second, preserving the
    ///   relative source order within each group. No other sort criterion is applied.
    /// </summary>
    /// <param name="projects"> The projects in their source (storage) order. </param>
    /// <returns> A new list with favorites before non-favorites. </returns>
    public static IReadOnlyList<Project> FavoritesFirst(IReadOnlyList<Project> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var favorites = new List<Project>(projects.Count);
        var others = new List<Project>(projects.Count);
        foreach (var project in projects)
        {
            if (project.IsFavorite)
            {
                favorites.Add(project);
            }
            else
            {
                others.Add(project);
            }
        }

        var ordered = new List<Project>(projects.Count);
        ordered.AddRange(favorites);
        ordered.AddRange(others);
        return ordered;
    }
}
