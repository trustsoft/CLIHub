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
    ///   Creates owned project state from the persisted application configuration; each
    ///   project is detached into its own copy so the state shares no references with the
    ///   configuration.
    /// </summary>
    /// <param name="config"> The configuration containing project state. </param>
    /// <returns> The owned project state with detached project copies. </returns>
    public static ProjectState From(AppConfig config)
    {
        var state = new ProjectState
        {
            CurrentProjectId = config.CurrentProjectId
        };
        state.Projects.AddRange(config.Projects.Select(project => project.Copy()));
        return state;
    }

    /// <summary>
    ///   Creates owned project state from a detached configuration snapshot; each project is
    ///   copied so the state shares no references with the snapshot.
    /// </summary>
    /// <param name="snapshot"> The configuration snapshot. </param>
    /// <returns> The owned project state with detached project copies. </returns>
    public static ProjectState From(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var state = new ProjectState
        {
            CurrentProjectId = snapshot.CurrentProjectId
        };
        state.Projects.AddRange(snapshot.Projects.Select(project => project.Copy()));
        return state;
    }

    /// <summary>
    ///   Copies the owned project state — each project detached — into the
    ///   persistence-facing configuration model.
    /// </summary>
    /// <param name="config"> The configuration to update. </param>
    public void ApplyTo(AppConfig config)
    {
        config.Projects = this.Projects.Select(project => project.Copy()).ToList();
        config.CurrentProjectId = this.CurrentProjectId;
    }

    /// <summary>
    ///   Copies the owned project state — each project detached — into a configuration
    ///   snapshot.
    /// </summary>
    /// <param name="snapshot"> The snapshot to update. </param>
    public void ApplyTo(ConfigurationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        snapshot.Projects = this.Projects.Select(project => project.Copy()).ToList();
        snapshot.CurrentProjectId = this.CurrentProjectId;
    }

    /// <summary>
    ///   Copies this state into another owned project state, detaching each project from
    ///   this state.
    /// </summary>
    /// <param name="target"> The state to replace. </param>
    public void CopyTo(ProjectState target)
    {
        ArgumentNullException.ThrowIfNull(target);

        target.Projects.Clear();
        target.Projects.AddRange(this.Projects.Select(project => project.Copy()));
        target.CurrentProjectId = this.CurrentProjectId;
    }
}
