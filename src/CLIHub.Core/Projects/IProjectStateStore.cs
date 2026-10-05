namespace CLIHub.Core.Projects;

using CLIHub.Core.Models;

/// <summary>
///   Provides project state access without exposing unrelated application preferences.
/// </summary>
public interface IProjectStateStore
{
    /// <summary>
    ///   Loads the current project state from the application configuration document.
    /// </summary>
    /// <returns> The owned project state. </returns>
    ProjectState Load();

    /// <summary>
    ///   Updates detached project state and persists it when the callback completes successfully.
    /// </summary>
    /// <param name="update"> Mutation applied to a detached project state value. </param>
    void Update(Action<ProjectState> update);
}
