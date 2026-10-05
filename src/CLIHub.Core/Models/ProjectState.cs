namespace CLIHub.Core.Models;

using CLIHub.Core.Configuration;

/// <summary>
///   Owned project state containing registered projects and the current selection.
/// </summary>
public sealed class ProjectState
{
    /// <summary>
    ///   Registered projects.
    /// </summary>
    public List<Project> Projects { get; } = new();

    /// <summary>
    ///   ID of the currently selected project, or null when no project is selected.
    /// </summary>
    public string? CurrentProjectId { get; set; }

    /// <summary>
    ///   Creates project state from the persisted application configuration.
    /// </summary>
    /// <param name="config"> The configuration containing project state. </param>
    /// <returns> The owned project state. </returns>
    public static ProjectState From(AppConfig config)
    {
        var state = new ProjectState
        {
            CurrentProjectId = config.CurrentProjectId
        };
        state.Projects.AddRange(config.Projects);
        return state;
    }

    /// <summary>
    ///   Creates project state from a detached configuration snapshot.
    /// </summary>
    /// <param name="snapshot"> The configuration snapshot. </param>
    /// <returns> The owned project state. </returns>
    public static ProjectState From(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var state = new ProjectState
        {
            CurrentProjectId = snapshot.CurrentProjectId
        };
        state.Projects.AddRange(snapshot.Projects);
        return state;
    }

    /// <summary>
    ///   Copies the owned project state into the persistence-facing configuration model.
    /// </summary>
    /// <param name="config"> The configuration to update. </param>
    public void ApplyTo(AppConfig config)
    {
        config.Projects = Projects;
        config.CurrentProjectId = CurrentProjectId;
    }

    /// <summary>
    ///   Copies the owned project state into a configuration snapshot.
    /// </summary>
    /// <param name="snapshot"> The snapshot to update. </param>
    public void ApplyTo(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshot.Projects = Projects;
        snapshot.CurrentProjectId = CurrentProjectId;
    }
}
