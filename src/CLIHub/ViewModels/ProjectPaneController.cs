namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

/// <summary>
///   Owns project-pane collection synchronization, selection, and project mutations.
/// </summary>
public sealed class ProjectPaneController : IDisposable
{
    private readonly IProjectService projectService;
    private readonly IProjectDialogService dialogs;
    private readonly PromptState promptState;
    private bool disposed;

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
        this.projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.promptState = promptState ?? throw new ArgumentNullException(nameof(promptState));
        this.projectService.ProjectsChanged += this.OnProjectsChanged;

        this.AddCommand = new RelayCommand(() => _ = this.AddProject());
        this.RemoveCommand = new RelayCommand(() => _ = this.RemoveSelectedProject(), () => this.CurrentProject is not null);
        this.ToggleFavoriteCommand = new RelayCommand(() => _ = this.ToggleFavorite(), () => this.CurrentProject is not null);
        this.RefreshCommand = new RelayCommand(() => _ = this.Refresh());
    }

    /// <summary>
    ///   Raised when an external project-service mutation changes the pane state.
    /// </summary>
    public event EventHandler? ProjectsChanged;

    /// <summary>
    ///   Command to add a new project through folder selection.
    /// </summary>
    public RelayCommand AddCommand { get; }

    /// <summary>
    ///   Command to remove the currently selected project after confirmation.
    /// </summary>
    public RelayCommand RemoveCommand { get; }

    /// <summary>
    ///   Command to toggle the favorite state of the currently selected project.
    /// </summary>
    public RelayCommand ToggleFavoriteCommand { get; }

    /// <summary>
    ///   Command to refresh the project list from the project service.
    /// </summary>
    public RelayCommand RefreshCommand { get; }

    /// <summary>
    ///   Projects currently shown in the project pane.
    /// </summary>
    public ObservableCollection<Project> Projects { get; } = new();

    /// <summary>
    ///   Returns the current project from the project service.
    /// </summary>
    public Project? CurrentProject => this.projectService.GetCurrentProject();

    /// <summary>
    ///   Synchronizes the collection while preserving existing project instances.
    /// </summary>
    /// <returns> The project selected by the project service, if it is visible. </returns>
    public Project? Refresh()
    {
        var projects = this.projectService.GetAllProjects();

        for (var index = this.Projects.Count - 1; index >= 0; index--)
        {
            if (projects.All(project => project.Id != this.Projects[index].Id))
            {
                this.Projects.RemoveAt(index);
            }
        }

        foreach (var project in projects)
        {
            if (this.Projects.All(existing => existing.Id != project.Id))
            {
                this.Projects.Add(project);
            }
        }

        var current = this.projectService.GetCurrentProject();
        return current is null
            ? null
            : this.Projects.FirstOrDefault(project => project.Id == current.Id);
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

        this.projectService.SetCurrentProject(project.Id);
        return true;
    }

    /// <summary>
    ///   Adds the folder selected by the user and makes it current.
    /// </summary>
    /// <returns> The added project, or null when folder selection was cancelled. </returns>
    public Project? AddProject()
    {
        using (this.promptState.Begin())
        {
            var folderPath = this.dialogs.SelectProjectFolder();
            if (folderPath is null)
            {
                return null;
            }

            var project = this.projectService.AddProject(folderPath);
            this.projectService.SetCurrentProject(project.Id);
            this.Refresh();
            return project;
        }
    }

    /// <summary>
    ///   Removes the current project after confirmation.
    /// </summary>
    /// <returns> True when a project was removed. </returns>
    public bool RemoveSelectedProject()
    {
        var project = this.CurrentProject;
        if (project is null)
        {
            return false;
        }

        using (this.promptState.Begin())
        {
            if (!this.dialogs.ConfirmProjectRemoval(project.Name))
            {
                return false;
            }
        }

        this.projectService.RemoveProject(project.Id);
        this.Refresh();
        return true;
    }

    /// <summary>
    ///   Toggles the favorite state of the current project.
    /// </summary>
    /// <returns> The current project after the mutation, or null when none is selected. </returns>
    public Project? ToggleFavorite()
    {
        var project = this.CurrentProject;
        if (project is null)
        {
            return null;
        }

        this.projectService.ToggleFavorite(project.Id);
        return project;
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        this.Refresh();
        ProjectsChanged?.Invoke(this, e);
    }

    /// <summary>
    ///   Removes the project-service event subscription.
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projectService.ProjectsChanged -= this.OnProjectsChanged;
    }
}
