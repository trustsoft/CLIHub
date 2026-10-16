namespace CLIHub.Launching;

/// <summary>
///   Provides project-related modal dialogs for the WPF application.
/// </summary>
public interface IProjectDialogService
{
    /// <summary>
    ///   Opens a folder picker and returns the selected path, or null when canceled.
    /// </summary>
    /// <returns> The selected project folder path, or null. </returns>
    string? SelectProjectFolder();

    /// <summary>
    ///   Opens a folder picker with a custom title and returns the selected path, or null when canceled.
    /// </summary>
    /// <param name="title"> The folder picker dialog title. </param>
    /// <returns> The selected folder path, or null. </returns>
    string? SelectFolder(string title);

    /// <summary>
    ///   Asks the user to confirm removing a project from CLIHub.
    /// </summary>
    /// <param name="projectName"> The project name shown in the confirmation message. </param>
    /// <returns> True when the user confirms removal. </returns>
    bool ConfirmProjectRemoval(string projectName);
}
