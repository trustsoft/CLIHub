namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

/// <summary>
///   Owns project-pane collection synchronization, selection, and project mutations.
/// </summary>
public sealed class ProjectPaneController : IDisposable
{
    private readonly IProjectService _projectService;
    private readonly IProjectDialogService _dialogs;
    private readonly PromptState _promptState;
    private bool _disposed;

    /// <summary>
    ///   Creates the project-pane workflow boundary.
    /// </summary>
    /// <param name="projectService"> Project persistence and selection service. </param>
    /// <param name="dialogs"> Project folder and removal confirmation dialogs. </param>
    /// <param name="promptState"> Modal prompt tracker used while dialogs are open. </param>
    public ProjectPaneController(
        IProjectService projectService,
        IProjectDialogService dialogs,
        PromptState promptState)
    {
        _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _promptState = promptState ?? throw new ArgumentNullException(nameof(promptState));
        _projectService.ProjectsChanged += OnProjectsChanged;
    }

    /// <summary>
    ///   Raised when an external project-service mutation changes the pane state.
    /// </summary>
    public event EventHandler? ProjectsChanged;

    /// <summary>
    ///   Projects currently shown in the project pane.
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>
    ///   Returns the current project from the project service.
    /// </summary>
    public Project? CurrentProject => _projectService.GetCurrentProject();

    /// <summary>
    ///   Synchronizes the collection while preserving existing project instances.
    /// </summary>
    /// <returns> The project selected by the project service, if it is visible. </returns>
    public Project? Refresh()
    {
        var projects = _projectService.GetAllProjects();

        for (var index = Projects.Count - 1; index >= 0; index--)
        {
            if (projects.All(project => project.Id != Projects[index].Id))
            {
                Projects.RemoveAt(index);
            }
        }

        foreach (var project in projects)
        {
            if (Projects.All(existing => existing.Id != project.Id))
            {
                Projects.Add(project);
            }
        }

        var current = _projectService.GetCurrentProject();
        return current is null
            ? null
            : Projects.FirstOrDefault(project => project.Id == current.Id);
    }

    /// <summary>
    ///   Selects a project and persists it as the current project.
    /// </summary>
    /// <param name="project"> Project to select, or null to clear selection. </param>
    /// <returns> True when a non-null project was persisted as current. </returns>
    public bool Select(Project? project)
    {
        if (project is null)
        {
            return false;
        }

        _projectService.SetCurrentProject(project.Id);
        return true;
    }

    /// <summary>
    ///   Adds the folder selected by the user and makes it current.
    /// </summary>
    /// <returns> The added project, or null when folder selection was cancelled. </returns>
    public Project? AddProject()
    {
        using (_promptState.Begin())
        {
            var folderPath = _dialogs.SelectProjectFolder();
            if (folderPath is null)
            {
                return null;
            }

            var project = _projectService.AddProject(folderPath);
            _projectService.SetCurrentProject(project.Id);
            Refresh();
            return project;
        }
    }

    /// <summary>
    ///   Removes the current project after confirmation.
    /// </summary>
    /// <returns> True when a project was removed. </returns>
    public bool RemoveSelectedProject()
    {
        var project = CurrentProject;
        if (project is null)
        {
            return false;
        }

        using (_promptState.Begin())
        {
            if (!_dialogs.ConfirmProjectRemoval(project.Name))
            {
                return false;
            }
        }

        _projectService.RemoveProject(project.Id);
        Refresh();
        return true;
    }

    /// <summary>
    ///   Toggles the favorite state of the current project.
    /// </summary>
    /// <returns> The current project after the mutation, or null when none is selected. </returns>
    public Project? ToggleFavorite()
    {
        var project = CurrentProject;
        if (project is null)
        {
            return null;
        }

        _projectService.ToggleFavorite(project.Id);
        return project;
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        Refresh();
        ProjectsChanged?.Invoke(this, e);
    }

    /// <summary>
    ///   Removes the project-service event subscription.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _projectService.ProjectsChanged -= OnProjectsChanged;
    }
}
