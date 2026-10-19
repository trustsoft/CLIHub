namespace CLIHub.ViewModels;

using CLIHub.Core.Projects;

/// <summary>
///   The only long-lived owner of <see cref="ProjectModel" /> rows, keyed by project ID and
///   shared by every project surface. <see cref="Sync" /> applies fresh enriched snapshots to
///   the existing instances in place, creates rows for new IDs, and drops rows whose project
///   vanished. The catalog owns instances, not surface orderings: each surface keeps its own
///   ordered collection referencing these rows.
/// </summary>
public sealed class ProjectModelCatalog
{
    private readonly Dictionary<string, ProjectModel> modelsById = new(StringComparer.Ordinal);

    /// <summary>
    ///   The rows currently alive, in unspecified order.
    /// </summary>
    public IReadOnlyCollection<ProjectModel> Models => this.modelsById.Values;

    /// <summary>
    ///   Synchronizes the catalog with fresh enriched snapshots: existing rows are updated in
    ///   place, new IDs get a row, and rows whose project is no longer present are dropped.
    /// </summary>
    /// <param name="snapshots"> The enriched snapshots in display order. </param>
    /// <returns> The rows for the snapshots, in the same order. </returns>
    public IReadOnlyList<ProjectModel> Sync(IReadOnlyList<EnrichedProject> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);

        var ordered = new List<ProjectModel>(snapshots.Count);
        var aliveIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var snapshot in snapshots)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (!this.modelsById.TryGetValue(snapshot.Project.Id, out var model))
            {
                model = new ProjectModel(snapshot);
                this.modelsById[model.Id] = model;
            }
            else
            {
                model.Update(snapshot);
            }

            aliveIds.Add(model.Id);
            ordered.Add(model);
        }

        foreach (var vanishedId in this.modelsById.Keys.Where(id => !aliveIds.Contains(id)).ToList())
        {
            this.modelsById.Remove(vanishedId);
        }

        return ordered;
    }
}
