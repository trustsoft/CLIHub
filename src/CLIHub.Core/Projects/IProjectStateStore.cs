namespace CLIHub.Core.Interfaces;

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
    ///   Persists project state through the shared configuration document and atomic write path.
    /// </summary>
    /// <param name="state"> The project state to persist. </param>
    void Save(ProjectState state);
}
