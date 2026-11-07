namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.IO;

using CLIHub.Core.Projects;
using CLIHub.Launching;

/// <summary>
///   Owns the Settings Projects page: the registered-project list, the path field and
///   add actions, the manual remove and change-folder actions, and the section error line.
///   Rows are the shared <see cref="ProjectModel" /> catalog instances, re-enriched on every
///   load; discovered projects are shown read-only.
/// </summary>
public sealed class ProjectRegistryViewModel : ObservableObject, IProjectRegistrySection
{
    private readonly IProjectService projects;
    private readonly ProjectEnricher enricher;
    private readonly ProjectModelCatalog catalog;
    private readonly IProjectDialogService dialogs;
    private readonly IExternalLauncher externalLauncher;

    private string newProjectPath = string.Empty;
    private string? errorText;
    private bool disposed;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="projects"> The project registry service. </param>
    /// <param name="enricher"> Produces enriched snapshots for the rows. </param>
    /// <param name="catalog"> Owner of the shared row instances. </param>
    /// <param name="dialogs"> Folder picker and removal confirmation. </param>
    /// <param name="externalLauncher"> Opens the project folder in the file manager. </param>
    public ProjectRegistryViewModel(IProjectService projects,
                                    ProjectEnricher enricher,
                                    ProjectModelCatalog catalog,
                                    IProjectDialogService dialogs,
                                    IExternalLauncher externalLauncher)
    {
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.enricher = enricher ?? throw new ArgumentNullException(nameof(enricher));
        this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));

        this.BrowseCommand = new RelayCommand(this.Browse);
        this.RegisterCommand = new RelayCommand(this.RegisterProject);
        this.RemoveCommand = new RelayCommand<ProjectModel>(this.RemoveProject, item => item.CanRemove);
        this.OpenFolderCommand = new RelayCommand<ProjectModel>(
            this.OpenFolder,
            item => item.CanOpen);
        this.ChangePathCommand = new RelayCommand<ProjectModel>(this.ChangePath, item => item.IsManual);
        this.projects.ProjectsChanged += this.OnProjectsChanged;
    }

    /// <inheritdoc />
    public ObservableCollection<ProjectModel> Projects { get; } = new();

    /// <inheritdoc />
    public int ManualCount => this.Projects.Count(item => item.IsManual);

    /// <inheritdoc />
    public int DiscoveredCount => this.Projects.Count(item => !item.IsManual);

    /// <inheritdoc />
    public int TotalCount => this.Projects.Count;

    /// <inheritdoc />
    public int MissingFolderCount => this.Projects.Count(item => !item.IsFolderAvailable);

    /// <inheritdoc />
    public bool HasMissingFolders => this.MissingFolderCount > 0;

    /// <inheritdoc />
    public string FolderStatusSummary => this.MissingFolderCount switch
    {
        0 => string.Empty,
        1 => "1 folder missing",
        var count => $"{count} folders missing"
    };

    /// <inheritdoc />
    public string MissingFolderChip => this.MissingFolderCount switch
    {
        0 => string.Empty,
        1 => "1 missing",
        var count => $"{count} missing"
    };

    /// <inheritdoc />
    public string BadgeTooltipText => this.HasMissingFolders
        ? $"{this.FolderStatusSummary} · folders that no longer exist"
        : string.Empty;

    /// <inheritdoc />
    public string NewProjectPath
    {
        get => this.newProjectPath;
        set => this.SetProperty(ref this.newProjectPath, value);
    }

    /// <inheritdoc />
    public string? ErrorText
    {
        get => this.errorText;
        private set
        {
            if (this.SetProperty(ref this.errorText, value))
            {
                this.OnPropertyChanged(nameof(this.HasError));
            }
        }
    }

    /// <inheritdoc />
    public bool HasError => !string.IsNullOrEmpty(this.ErrorText);

    /// <inheritdoc />
    public RelayCommand BrowseCommand { get; }

    /// <inheritdoc />
    public RelayCommand RegisterCommand { get; }

    /// <inheritdoc />
    public RelayCommand<ProjectModel> RemoveCommand { get; }

    /// <inheritdoc />
    public RelayCommand<ProjectModel> OpenFolderCommand { get; }

    /// <summary>
    ///   Changes the folder of a manual project: opens the folder picker, applies the new path
    ///   through the project service, and re-loads the enriched rows. Unavailable for discovered
    ///   projects, which Project Scanning manages.
    /// </summary>
    public RelayCommand<ProjectModel> ChangePathCommand { get; }

    /// <inheritdoc />
    public void Load()
    {
        if (this.disposed)
        {
            return;
        }

        this.ErrorText = null;

        var models = this.catalog.Sync(this.enricher.Enrich(this.projects.GetAllProjects()));
        var ordered = models
            .OrderByDescending(model => model.IsManual)
            .ThenBy(model => model.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(model => model.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        ProjectModelListSynchronizer.Synchronize(this.Projects, ordered);

        this.RaiseSummaryChanges();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;
        this.projects.ProjectsChanged -= this.OnProjectsChanged;
    }

    private void Browse()
    {
        var folder = this.dialogs.SelectFolder("Add Project Folder");
        if (!string.IsNullOrWhiteSpace(folder))
        {
            this.NewProjectPath = folder;
        }
    }

    private void RegisterProject()
    {
        if (this.TryRegisterProject(this.NewProjectPath))
        {
            this.NewProjectPath = string.Empty;
        }
    }

    private bool TryRegisterProject(string value)
    {
        var text = value.Trim();
        if (text.Length == 0)
        {
            this.ErrorText = "Choose a folder or type its path.";
            return false;
        }

        if (!Path.IsPathFullyQualified(text) || !Directory.Exists(text))
        {
            this.ErrorText = "Use a full folder path that exists.";
            return false;
        }

        string path;
        try
        {
            path = ProjectPathPolicy.Normalize(text);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            this.ErrorText = "That path isn't valid.";
            return false;
        }

        if (this.Projects.Any(item => ProjectPathPolicy.AreEqual(item.Path, path)))
        {
            this.ErrorText = "This folder is already registered.";
            return false;
        }

        try
        {
            this.projects.AddProject(path);
            this.Load();
            return true;
        }
        catch (Exception ex)
        {
            this.ErrorText = $"Could not add project: {ex.Message}";
            return false;
        }
    }

    private void OpenFolder(ProjectModel item)
    {
        if (item is null || !item.CanOpen)
        {
            return;
        }

        try
        {
            this.externalLauncher.Open(item.Path);
        }
        catch (Exception ex)
        {
            this.ErrorText = $"Could not open folder: {ex.Message}";
        }
    }

    private void ChangePath(ProjectModel item)
    {
        if (item is null || !item.IsManual)
        {
            return;
        }

        this.ErrorText = null;

        var folder = this.dialogs.SelectFolder("Change Project Folder");
        if (string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            this.projects.ChangeProjectPath(item.Id, folder);
            this.Load();
        }
        catch (DirectoryNotFoundException ex)
        {
            this.ErrorText = $"Folder not found: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            this.ErrorText = $"Could not change project path: {ex.Message}";
        }
        catch (Exception ex)
        {
            this.ErrorText = $"Could not change project path: {ex.Message}";
        }
    }

    private void RemoveProject(ProjectModel item)
    {
        if (item is null || !item.CanRemove)
        {
            return;
        }

        this.ErrorText = null;

        if (!this.dialogs.ConfirmProjectRemoval(item.Name))
        {
            return;
        }

        try
        {
            this.projects.RemoveProject(item.Id);
            this.Load();
        }
        catch (Exception ex)
        {
            this.ErrorText = $"Could not remove project: {ex.Message}";
        }
    }

    private void OnProjectsChanged(object? sender, EventArgs e) => this.Load();

    private void RaiseSummaryChanges()
    {
        this.OnPropertyChanged(nameof(this.ManualCount));
        this.OnPropertyChanged(nameof(this.DiscoveredCount));
        this.OnPropertyChanged(nameof(this.TotalCount));
        this.OnPropertyChanged(nameof(this.MissingFolderCount));
        this.OnPropertyChanged(nameof(this.HasMissingFolders));
        this.OnPropertyChanged(nameof(this.FolderStatusSummary));
        this.OnPropertyChanged(nameof(this.MissingFolderChip));
        this.OnPropertyChanged(nameof(this.BadgeTooltipText));
    }
}
