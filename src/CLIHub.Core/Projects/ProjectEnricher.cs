namespace CLIHub.Core.Projects;

using CLIHub.Core.Models;

/// <summary>
///   Produces enriched project snapshots: one folder-existence check plus one logo-resolver
///   lookup per project. The enricher is stateless — freshness is owned by the callers that
///   re-run it on their refresh triggers (project set changes, window show/activate, Settings
///   page load). There is no folder watching and no timer.
/// </summary>
public sealed class ProjectEnricher
{
    private readonly ProjectLogoResolver logoResolver;

    /// <summary>
    ///   Creates the enricher over the shared project logo resolver.
    /// </summary>
    /// <param name="logoResolver"> The resolver supplying final logo paths (fallback applied). </param>
    public ProjectEnricher(ProjectLogoResolver logoResolver)
    {
        this.logoResolver = logoResolver ?? throw new ArgumentNullException(nameof(logoResolver));
    }

    /// <summary>
    ///   Enriches each project with its folder existence and final logo path, preserving the
    ///   input order. Logo lookups go through the persistent logo cache, so repeat enrichments
    ///   do not rescan project folders.
    /// </summary>
    /// <param name="projects"> The projects to enrich. </param>
    /// <returns> One enriched snapshot per input project, in input order. </returns>
    public IReadOnlyList<EnrichedProject> Enrich(IReadOnlyList<Project> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var snapshots = new List<EnrichedProject>(projects.Count);
        foreach (var project in projects)
        {
            snapshots.Add(new EnrichedProject(
                project,
                Directory.Exists(project.Path),
                this.logoResolver.Resolve(project)));
        }

        return snapshots;
    }
}
