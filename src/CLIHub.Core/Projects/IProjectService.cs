namespace CLIHub.Core.Projects;

using CLIHub.Core.Models;

/// <summary>
///   Tracks project directories, the current selection, recency, and favorites.
/// </summary>
public interface IProjectService
{
    /// <summary>
    ///   Raised whenever the set of projects or the current selection changes.
    /// </summary>
    event EventHandler? ProjectsChanged;

    /// <summary>
    ///   Returns all registered projects as detached copies: mutating a returned project changes
    ///   neither later reads nor persisted state.
    /// </summary>
    IReadOnlyList<Project> GetAllProjects();

    /// <summary>
    ///   Returns projects ordered by last-used descending, up to <paramref name="limit" />, as
    ///   detached copies (see <see cref="GetAllProjects" />).
    /// </summary>
    IReadOnlyList<Project> GetRecentProjects(int limit);

    /// <summary>
    ///   Returns the currently selected project as a detached copy, or null if none is selected
    ///   (see <see cref="GetAllProjects" />).
    /// </summary>
    Project? GetCurrentProject();

    /// <summary>
    ///   Registers a project folder as a manual project. Returns the existing project
    ///   if the folder is already registered (without changing its source).
    /// </summary>
    /// <exception cref="ArgumentException"> The folder path is null/empty. </exception>
    /// <exception cref="DirectoryNotFoundException"> The folder does not exist. </exception>
    Project AddProject(string folderPath);

    /// <summary>
    ///   Registers a discovered project folder. Returns the existing project
    ///   if the folder is already registered (without reclassifying a manual project).
    /// </summary>
    /// <exception cref="ArgumentException"> The folder path is null/empty. </exception>
    /// <exception cref="DirectoryNotFoundException"> The folder does not exist. </exception>
    Project AddDiscoveredProject(string folderPath);

    /// <summary>
    ///   Reconciles the discovered-project subset: adds each discovered folder that is not yet
    ///   registered and removes the listed discovered projects. Manually registered projects and
    ///   unrelated projects are left untouched.
    /// </summary>
    /// <param name="discoveredPaths"> Folder paths discovered by scanning. </param>
    /// <param name="removedProjectIds"> IDs of discovered projects to remove. </param>
    /// <returns> True when the project set changed. </returns>
    bool ReconcileDiscoveredProjects(IReadOnlyCollection<string> discoveredPaths,
                                     IReadOnlyCollection<string> removedProjectIds);

    /// <summary>
    ///   Removes a project from configuration. Does not delete files on disk.
    /// </summary>
    void RemoveProject(string projectId);

    /// <summary>
    ///   Sets the current project and updates its last-used timestamp.
    /// </summary>
    void SetCurrentProject(string projectId);

    /// <summary>
    ///   Toggles the favorite flag for a project.
    /// </summary>
    void ToggleFavorite(string projectId);
}
