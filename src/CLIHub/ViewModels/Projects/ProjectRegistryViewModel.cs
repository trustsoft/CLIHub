namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;
using System.IO;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;
using CLIHub.Launching;

/// <summary>
///   Owns the Settings Projects page: the registered-project list, the path field and
///   add actions, the manual remove action, and the section error line. Discovered
///   projects are shown read-only.
/// </summary>
public sealed class ProjectRegistryViewModel : ObservableObject, IProjectRegistrySection
{
    private readonly IProjectService projects;
    private readonly IProjectDialogService dialogs;
    private readonly IExternalLauncher externalLauncher;

    private string newProjectPath = string.Empty;
    private string? errorText;
    private bool disposed;

    /// <summary>
    ///   Creates the section.
    /// </summary>
    /// <param name="projects"> The project registry service. </param>
    /// <param name="dialogs"> Folder picker and removal confirmation. </param>
    /// <param name="externalLauncher"> Opens the project folder in the file manager. </param>
    public ProjectRegistryViewModel(IProjectService projects,
                                    IProjectDialogService dialogs,
                                    IExternalLauncher externalLauncher)
    {
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.externalLauncher = externalLauncher ?? throw new ArgumentNullException(nameof(externalLauncher));

        this.BrowseCommand = new RelayCommand(this.Browse);
        this.RegisterCommand = new RelayCommand(this.RegisterProject);
        this.RemoveCommand = new RelayCommand<ProjectRegistryItem>(this.RemoveProject, item => item.IsManual);
        this.OpenFolderCommand = new RelayCommand<ProjectRegistryItem>(
            this.OpenFolder,
            item => !item.IsFolderMissing);
        this.projects.ProjectsChanged += this.OnProjectsChanged;
    }

    /// <inheritdoc />
    public ObservableCollection<ProjectRegistryItem> Projects { get; } = new();

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
    public RelayCommand<ProjectRegistryItem> RemoveCommand { get; }

    /// <inheritdoc />
    public RelayCommand<ProjectRegistryItem> OpenFolderCommand { get; }

    /// <inheritdoc />
    public void Load()
    {
        if (this.disposed)
        {
            return;
        }

        this.ErrorText = null;
        this.Projects.Clear();

        var ordered = this.projects.GetAllProjects()
            .OrderByDescending(project => project.Source == ProjectSource.Manual)
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.Path, StringComparer.OrdinalIgnoreCase);

        foreach (var project in ordered)
        {
            this.Projects.Add(new ProjectRegistryItem(project, !Directory.Exists(project.Path)));
        }
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
            this.ErrorText = "Enter a folder path or browse for one.";
            return false;
        }

        if (!Path.IsPathFullyQualified(text) || !Directory.Exists(text))
        {
            this.ErrorText = "Enter an existing absolute folder path.";
            return false;
        }

        string path;
        try
        {
            path = Path.TrimEndingDirectorySeparator(Path.GetFullPath(text));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            this.ErrorText = "Enter a valid absolute folder path.";
            return false;
        }

        if (this.Projects.Any(item => string.Equals(item.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            this.ErrorText = "This folder is already registered as a project.";
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

    private void OpenFolder(ProjectRegistryItem item)
    {
        if (item is null || item.IsFolderMissing)
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

    private void RemoveProject(ProjectRegistryItem item)
    {
        if (item is null || !item.IsManual)
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
}
