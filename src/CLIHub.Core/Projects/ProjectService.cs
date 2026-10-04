namespace CLIHub.Core.Services;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
///   Tracks project directories backed by <see cref="IConfigService"/>.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly IProjectStateStore _projectStateStore;
    private readonly ProjectLogoResolver _logoResolver;
    private readonly ILogger<ProjectService> _logger;
    private readonly ProjectState _projectState;

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
        _projectStateStore = projectStateStore ?? throw new ArgumentNullException(nameof(projectStateStore));
        _logoResolver = new ProjectLogoResolver(logoCache);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _projectState = _projectStateStore.Load();
        ClearStaleCurrentProject();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetAllProjects()
    {
        RefreshLogos();
        return _projectState.Projects.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetRecentProjects(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<Project>();
        }

        RefreshLogos();
        return _projectState.Projects
            .OrderByDescending(p => p.LastUsed)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public Project? GetCurrentProject()
    {
        if (_projectState.CurrentProjectId == null)
        {
            return null;
        }

        var project = _projectState.Projects.FirstOrDefault(p => p.Id == _projectState.CurrentProjectId);
        if (project != null)
        {
            project.LogoPath = ResolveProjectLogo(project);
        }

        return project;
    }

    private void RefreshLogos()
    {
        foreach (var project in _projectState.Projects)
        {
            project.LogoPath = ResolveProjectLogo(project);
        }
    }

    /// <summary>
    ///   Resolves the project logo through the logo cache keyed by the project ID, falling back
    ///   to <see cref="DefaultLogoPath"/> when no logo is found.
    /// </summary>
    private string? ResolveProjectLogo(Project project) =>
        _logoResolver.Resolve(project, DefaultLogoPath);

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

        var existing = _projectState.Projects.FirstOrDefault(p => ProjectPathPolicy.AreEqual(p.Path, normalized));
        if (existing != null)
        {
            return existing;
        }

        var project = new Project
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = Path.GetFileName(normalized.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
            Path = normalized,
            IsFavorite = false,
            LastUsed = DateTime.UtcNow
        };

        if (string.IsNullOrEmpty(project.Name))
        {
            project.Name = normalized;
        }

        project.LogoPath = ResolveProjectLogo(project);

        _projectState.Projects.Add(project);
        _logger.LogInformation("Added project {ProjectName} at {Path}", project.Name, project.Path);
        Persist();
        return project;
    }

    /// <inheritdoc />
    public void RemoveProject(string projectId)
    {
        var project = _projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        _projectState.Projects.Remove(project);
        _logoResolver.Remove(projectId);
        _logger.LogInformation("Removed project {ProjectName}", project.Name);

        if (_projectState.CurrentProjectId == projectId)
        {
            _projectState.CurrentProjectId = null;
        }

        Persist();
    }

    /// <inheritdoc />
    public void SetCurrentProject(string projectId)
    {
        var project = _projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        _projectState.CurrentProjectId = projectId;
        project.LastUsed = DateTime.UtcNow;
        _logger.LogDebug("Current project set to {ProjectName}", project.Name);
        Persist();
    }

    /// <inheritdoc />
    public void ToggleFavorite(string projectId)
    {
        var project = _projectState.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        project.IsFavorite = !project.IsFavorite;
        Persist();
    }

    /// <summary>
    ///   Resolves a logo for the project folder, falling back to <see cref="DefaultLogoPath"/>.
    /// </summary>
    internal string? ResolveLogo(string projectPath) => _logoResolver.ResolvePath(projectPath, DefaultLogoPath);

    private void ClearStaleCurrentProject()
    {
        if (_projectState.CurrentProjectId == null)
        {
            return;
        }

        var matches = _projectState.Projects.Any(p => p.Id == _projectState.CurrentProjectId);
        if (!matches)
        {
            _logger.LogDebug("Clearing stale current project id {ProjectId}", _projectState.CurrentProjectId);
            _projectState.CurrentProjectId = null;
            Persist();
        }
    }

    private void Persist()
    {
        _projectStateStore.Save(_projectState);
        ProjectsChanged?.Invoke(this, EventArgs.Empty);
    }

}
