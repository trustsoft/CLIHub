namespace CLIHub.Core.Interfaces;

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
    ///   Default logo path used when a project has no detectable logo. May be null.
    /// </summary>
    string? DefaultLogoPath { get; set; }

    /// <summary>
    ///   Returns all registered projects.
    /// </summary>
    IReadOnlyList<Project> GetAllProjects();

    /// <summary>
    ///   Returns projects ordered by last-used descending, up to <paramref name="limit"/>.
    /// </summary>
    IReadOnlyList<Project> GetRecentProjects(int limit);

    /// <summary>
    ///   Returns projects marked as favorites.
    /// </summary>
    IReadOnlyList<Project> GetFavorites();

    /// <summary>
    ///   Returns the currently selected project, or null if none is selected.
    /// </summary>
    Project? GetCurrentProject();

    /// <summary>
    ///   Registers a project folder. Returns the existing project if the folder is already registered.
    /// </summary>
    /// <exception cref="ArgumentException"> The folder path is null/empty. </exception>
    /// <exception cref="DirectoryNotFoundException"> The folder does not exist. </exception>
    Project AddProject(string folderPath);

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

    /// <summary>
    ///   Updates a project's last-used timestamp without changing the current selection.
    /// </summary>
    void TouchProject(string projectId);

    /// <summary>
    ///   Resolves a logo for the project folder, falling back to <see cref="DefaultLogoPath"/>.
    /// </summary>
    string? ResolveLogo(string projectPath);
}
