namespace CLIHub.Core.Projects;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;

/// <summary>
///   Tracks project directories backed by the configuration repository.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectStateStore projectStateStore;
    private readonly ProjectLogoResolver logoResolver;
    private readonly ILogger<ProjectService> logger;
    private readonly ProjectState projectState;

    /// <inheritdoc />
    public event EventHandler? ProjectsChanged;

    /// <inheritdoc />
    public string? DefaultLogoPath { get; set; }

    /// <summary>
    ///   Creates the service with the default project logo path.
    /// </summary>
    public ProjectService(
        IProjectStateStore projectStateStore,
        ILogoCacheService logoCache,
        ILogger<ProjectService> logger)
    {
        this.projectStateStore = projectStateStore ?? throw new ArgumentNullException(nameof(projectStateStore));
        this.logoResolver = new ProjectLogoResolver(logoCache);
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.projectState = this.projectStateStore.Load();
        this.ClearStaleCurrentProject();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetAllProjects()
    {
        this.RefreshLogos();
        return this.projectState.Projects.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetRecentProjects(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<Project>();
        }

        this.RefreshLogos();
        return this.projectState.Projects
            .OrderByDescending(p => p.LastUsed)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public Project? GetCurrentProject()
    {
        if (this.projectState.CurrentProjectId == null)
        {
            return null;
        }

        var project = this.projectState.Projects.FirstOrDefault(p => p.Id == this.projectState.CurrentProjectId);
        if (project != null)
        {
            project.LogoPath = this.ResolveProjectLogo(project);
        }

        return project;
    }

    private void RefreshLogos()
    {
        foreach (var project in this.projectState.Projects)
        {
            project.LogoPath = this.ResolveProjectLogo(project);
        }
    }

    /// <summary>
    ///   Resolves the project logo through the logo cache keyed by the project ID,
    ///   falling back to <see cref="DefaultLogoPath" /> when no logo is found.
    /// </summary>
    private string? ResolveProjectLogo(Project project) =>
        this.logoResolver.Resolve(project, this.DefaultLogoPath);

    /// <inheritdoc />
    public Project AddProject(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        var normalized = ProjectPathPolicy.Normalize(folderPath);

        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {normalized}");
        }

        var existing = this.projectState.Projects.FirstOrDefault(p => ProjectPathPolicy.AreEqual(p.Path, normalized));
        if (existing != null)
        {
            return existing;
        }

        var project = this.CreateProject(normalized, ProjectSource.Manual);
        this.projectState.Projects.Add(project);
        this.logger.LogInformation("Added project {ProjectName} at {Path}", project.Name, project.Path);
        this.Persist();
        return project;
    }

    /// <inheritdoc />
    public Project AddDiscoveredProject(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        var normalized = ProjectPathPolicy.Normalize(folderPath);

        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {normalized}");
        }

        var existing = this.projectState.Projects.FirstOrDefault(p => ProjectPathPolicy.AreEqual(p.Path, normalized));
        if (existing != null)
        {
            return existing;
        }

        var project = this.CreateProject(normalized, ProjectSource.Discovered);
        this.projectState.Projects.Add(project);
        this.logger.LogInformation("Discovered project {ProjectName} at {Path}", project.Name, project.Path);
        this.Persist();
        return project;
    }

    /// <inheritdoc />
    public bool ReconcileDiscoveredProjects(
        IReadOnlyCollection<string> discoveredPaths,
        IReadOnlyCollection<string> removedProjectIds)
    {
        ArgumentNullException.ThrowIfNull(discoveredPaths);
        ArgumentNullException.ThrowIfNull(removedProjectIds);

        var changed = false;

        foreach (var path in discoveredPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var normalized = ProjectPathPolicy.Normalize(path);

            if (this.projectState.Projects.Any(p => ProjectPathPolicy.AreEqual(p.Path, normalized)))
            {
                continue;
            }

            if (!Directory.Exists(normalized))
            {
                continue;
            }

            this.projectState.Projects.Add(this.CreateProject(normalized, ProjectSource.Discovered));
            changed = true;
        }

        foreach (var id in removedProjectIds)
        {
            var project = this.projectState.Projects.FirstOrDefault(p => p.Id == id && p.Source == ProjectSource.Discovered);
            if (project is null)
            {
                continue;
            }

            this.projectState.Projects.Remove(project);
            this.logoResolver.Remove(id);

            if (this.projectState.CurrentProjectId == id)
            {
                this.projectState.CurrentProjectId = null;
            }

            changed = true;
        }

        if (changed)
        {
            this.logger.LogInformation(
                "Reconciled discovered projects: {AddedOrRemoved} change(s)",
                discoveredPaths.Count + removedProjectIds.Count);
            this.Persist();
        }

        return changed;
    }

    /// <summary>
    ///   Creates a project entry for a normalized folder path without adding it to the state.
    /// </summary>
    private Project CreateProject(string normalized, ProjectSource source)
    {
        var project = new Project
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Path.GetFileName(normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            Path = normalized,
            Source = source,
            IsFavorite = false,
            LastUsed = DateTime.UtcNow
        };

        if (string.IsNullOrEmpty(project.Name))
        {
            project.Name = normalized;
        }

        project.LogoPath = this.ResolveProjectLogo(project);
        return project;
    }

    /// <inheritdoc />
    public void RemoveProject(string projectId)
    {
        var project = this.projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        this.projectState.Projects.Remove(project);
        this.logoResolver.Remove(projectId);
        this.logger.LogInformation("Removed project {ProjectName}", project.Name);

        if (this.projectState.CurrentProjectId == projectId)
        {
            this.projectState.CurrentProjectId = null;
        }

        this.Persist();
    }

    /// <inheritdoc />
    public void SetCurrentProject(string projectId)
    {
        var project = this.projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        this.projectState.CurrentProjectId = projectId;
        project.LastUsed = DateTime.UtcNow;
        this.logger.LogDebug("Current project set to {ProjectName}", project.Name);
        this.Persist();
    }

    /// <inheritdoc />
    public void ToggleFavorite(string projectId)
    {
        var project = this.projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        project.IsFavorite = !project.IsFavorite;
        this.Persist();
    }

    /// <summary>
    ///   Resolves a logo for the project folder, falling back to <see cref="DefaultLogoPath" />.
    /// </summary>
    internal string? ResolveLogo(string projectPath) => this.logoResolver.ResolvePath(projectPath, this.DefaultLogoPath);

    private void ClearStaleCurrentProject()
    {
        if (this.projectState.CurrentProjectId == null)
        {
            return;
        }

        var matches = this.projectState.Projects.Any(p => p.Id == this.projectState.CurrentProjectId);
        if (!matches)
        {
            this.logger.LogDebug("Clearing stale current project id {ProjectId}", this.projectState.CurrentProjectId);
            this.projectState.CurrentProjectId = null;
            this.Persist();
        }
    }

    private void Persist()
    {
        this.projectStateStore.Update(state => this.projectState.CopyTo(state));
        this.ProjectsChanged?.Invoke(this, EventArgs.Empty);
    }

}
