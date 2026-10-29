namespace CLIHub.Core.Projects;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Events;
using CLIHub.Core.Models;

/// <summary>
///   Tracks project directories backed by the configuration repository.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectStateStore projectStateStore;
    private readonly ProjectLogoResolver logoResolver;
    private readonly ILogger<ProjectService> logger;
    private readonly IDomainEventPublisher eventPublisher;
    private readonly ProjectState projectState;

    /// <inheritdoc />
    public event EventHandler? ProjectsChanged;

    /// <summary>
    ///   Creates the service with the project logo resolver.
    /// </summary>
    public ProjectService(IProjectStateStore projectStateStore,
                          ProjectLogoResolver logoResolver,
                          ILogger<ProjectService> logger,
                          IDomainEventPublisher eventPublisher)
    {
        this.projectStateStore = projectStateStore ?? throw new ArgumentNullException(nameof(projectStateStore));
        this.logoResolver = logoResolver ?? throw new ArgumentNullException(nameof(logoResolver));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        this.projectState = this.projectStateStore.Load();
        this.ClearStaleCurrentProject();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetAllProjects() =>
            this.projectState.Projects.Select(project => project.Copy()).ToList();

    /// <inheritdoc />
    public IReadOnlyList<Project> GetRecentProjects(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<Project>();
        }

        return this.projectState.Projects
            .OrderByDescending(p => p.LastUsed)
            .Take(limit)
            .Select(p => p.Copy())
            .ToList();
    }

    /// <inheritdoc />
    public Project? GetCurrentProject()
    {
        if (this.projectState.CurrentProjectId == null)
        {
            return null;
        }

        return this.projectState.Projects
            .FirstOrDefault(p => p.Id == this.projectState.CurrentProjectId)
            ?.Copy();
    }

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
            return existing.Copy();
        }

        var project = this.CreateProject(normalized, ProjectSource.Manual);
        this.projectState.Projects.Add(project);
        this.logger.LogInformation("Added project {ProjectName} at {Path}", project.Name, project.Path);
        this.Persist();
        return project.Copy();
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
            return existing.Copy();
        }

        var project = this.CreateProject(normalized, ProjectSource.Discovered);
        this.projectState.Projects.Add(project);
        this.logger.LogInformation("Discovered project {ProjectName} at {Path}", project.Name, project.Path);
        this.Persist();
        return project.Copy();
    }

    /// <inheritdoc />
    public bool ReconcileDiscoveredProjects(IReadOnlyCollection<string> discoveredPaths,
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

        var selectionChanged = projectId != this.projectState.CurrentProjectId;

        this.projectState.CurrentProjectId = projectId;
        project.LastUsed = DateTime.UtcNow;
        this.logger.LogDebug("Current project set to {ProjectName}", project.Name);
        this.Persist();

        if (selectionChanged)
        {
            this.eventPublisher.Publish(new ProjectSelected(projectId, project.Path, DateTimeOffset.UtcNow));
        }
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
