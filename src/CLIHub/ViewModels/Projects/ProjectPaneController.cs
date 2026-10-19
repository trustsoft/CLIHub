namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

/// <summary>
///   Owns project-pane collection synchronization, selection, and project mutations. Rows are
///   the shared <see cref="ProjectModel" /> instances owned by the <see cref="ProjectModelCatalog" />,
///   re-enriched through the <see cref="ProjectEnricher" /> on every refresh.
/// </summary>
public sealed class ProjectPaneController : IDisposable
{
    private readonly IProjectService projectService;
    private readonly ProjectEnricher enricher;
    private readonly ProjectModelCatalog catalog;
    private readonly IProjectDialogService dialogs;
    private readonly PromptState promptState;
    private bool disposed;

    /// <summary>
    ///   Creates the project-pane workflow boundary.
    /// </summary>
    /// <param name="projectService"> Project persistence and selection service. </param>
    /// <param name="enricher"> Produces enriched snapshots for the pane rows. </param>
    /// <param name="catalog"> Owner of the shared row instances. </param>
    /// <param name="dialogs"> Project folder and removal confirmation dialogs. </param>
    /// <param name="promptState"> Modal prompt tracker used while dialogs are open. </param>
    public ProjectPaneController(IProjectService projectService,
                                 ProjectEnricher enricher,
                                 ProjectModelCatalog catalog,
                                 IProjectDialogService dialogs,
                                 PromptState promptState)
    {
        this.projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
        this.enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.promptState = promptState ?? throw new ArgumentNullException(nameof(promptState));
        this.projectService.ProjectsChanged += this.OnProjectsChanged;

        this.AddCommand = new RelayCommand(() => _ = this.AddProject());
        this.RemoveCommand = new RelayCommand(() => _ = this.RemoveSelectedProject(), () => this.GetCurrentProject() is not null);
        this.ToggleFavoriteCommand = new RelayCommand(this.ToggleFavorite, () => this.GetCurrentProject() is not null);
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
    ///   Projects currently shown in the project pane, referencing the shared catalog rows.
    /// </summary>
    public ObservableCollection<ProjectModel> Projects { get; } = new();

    /// <summary>
    ///   Re-reads the projects, re-enriches them, and synchronizes the pane rows in place.
    /// </summary>
    /// <returns> The row matching the project service's current project, if it is visible. </returns>
    public ProjectModel? Refresh()
    {
        var models = this.catalog.Sync(this.enricher.Enrich(this.projectService.GetAllProjects()));
        ProjectModelListSynchronizer.Synchronize(this.Projects, models);

        var currentId = this.GetCurrentProject()?.Id;
        return currentId is null
            ? null
            : models.FirstOrDefault(model => model.Id == currentId);
    }

    /// <summary>
    ///   Selects a project and persists it as the current project.
    /// </summary>
    /// <param name="project"> Project row to select, or null to clear selection. </param>
    /// <returns> True when a non-null project was persisted as current. </returns>
    public bool Select(ProjectModel? project)
    {
        if (project is null)
        {
            return false;
        }

        this.projectService.SetCurrentProject(project.Id);
        return true;
    }

    /// <summary>
    ///   Looks up a registered project by ID, detached from the pane rows. Used to resolve the
    ///   launch context at command time instead of holding a row instance.
    /// </summary>
    /// <param name="projectId"> The project identifier. </param>
    /// <returns> The registered project, or null when the ID is unknown. </returns>
    public Project? FindProject(string projectId) =>
            this.projectService.GetAllProjects().FirstOrDefault(project => project.Id == projectId);

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
        var project = this.GetCurrentProject();
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
    public void ToggleFavorite()
    {
        var project = this.GetCurrentProject();
        if (project is null)
        {
            return;
        }

        this.projectService.ToggleFavorite(project.Id);
    }

    private void OnProjectsChanged(object? sender, EventArgs e)
    {
        this.Refresh();
        this.ProjectsChanged?.Invoke(this, e);
    }

    private Project? GetCurrentProject() => this.projectService.GetCurrentProject();

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
