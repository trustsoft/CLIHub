namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

/// <summary>
///   The project-registry section of the Settings window: every registered project,
///   the manual add action, the manual remove action, and the section error line.
/// </summary>
public interface IProjectRegistrySection : IDisposable
{
    /// <summary>
    ///   Registered projects: manual first, then discovered, each ordered by name. The rows are
    ///   the shared enriched display models, updated in place on refresh.
    /// </summary>
    ObservableCollection<ProjectModel> Projects { get; }

    /// <summary>
    ///   Number of manually registered projects.
    /// </summary>
    int ManualCount { get; }

    /// <summary>
    ///   Number of discovered projects.
    /// </summary>
    int DiscoveredCount { get; }

    /// <summary>
    ///   Total number of registered projects.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    ///   Number of projects whose folder no longer exists.
    /// </summary>
    int MissingFolderCount { get; }

    /// <summary>
    ///   Whether any project folder is missing.
    /// </summary>
    bool HasMissingFolders { get; }

    /// <summary>
    ///   The discovered/manual split summary, e.g. "31 discovered · 4 manual".
    /// </summary>
    string ProjectMixSummary { get; }

    /// <summary>
    ///   The folder-health summary shown under the split.
    /// </summary>
    string FolderStatusSummary { get; }

    /// <summary>
    ///   The error from the last add or remove action, or null.
    /// </summary>
    string? ErrorText { get; }

    /// <summary>
    ///   Whether there is an error to show.
    /// </summary>
    bool HasError { get; }

    /// <summary>
    ///   The folder path typed or selected for registration.
    /// </summary>
    string NewProjectPath { get; set; }

    /// <summary>
    ///   Command that fills <see cref="NewProjectPath" /> from the folder picker.
    /// </summary>
    RelayCommand BrowseCommand { get; }

    /// <summary>
    ///   Command that registers the typed or selected folder as a manual project.
    /// </summary>
    RelayCommand RegisterCommand { get; }

    /// <summary>
    ///   Command that removes a manual project after confirmation.
    /// </summary>
    RelayCommand<ProjectModel> RemoveCommand { get; }

    /// <summary>
    ///   Command that opens the project folder in the operating system's file manager.
    /// </summary>
    RelayCommand<ProjectModel> OpenFolderCommand { get; }

    /// <summary>
    ///   Re-enriches the rows from the project service and synchronizes the collection in place.
    /// </summary>
    void Load();
}
