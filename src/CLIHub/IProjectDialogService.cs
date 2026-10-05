namespace CLIHub;

/// <summary>
///   Provides project-related modal dialogs for the WPF application.
/// </summary>
public interface IProjectDialogService
{
    /// <summary>
    ///   Opens a folder picker and returns the selected path, or null when cancelled.
    /// </summary>
    /// <returns> The selected project folder path, or null. </returns>
    string? SelectProjectFolder();

    /// <summary>
    ///   Asks the user to confirm removing a project from CLIHub.
    /// </summary>
    /// <param name="projectName"> The project name shown in the confirmation message. </param>
    /// <returns> True when the user confirms removal. </returns>
    bool ConfirmProjectRemoval(string projectName);
}
