namespace CLIHub.Core.Services;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
///   Tracks project directories backed by <see cref="IConfigService"/>.
/// </summary>
public class ProjectService : IProjectService
{
    private static readonly string[] CandidateLogoNames =
    {
        "logo.png", "logo.jpg", "logo.jpeg", "logo.svg",
        "icon.png", "icon.jpg", "favicon.png"
    };

    private readonly IConfigService _configService;
    private readonly ILogoCacheService _logoCache;
    private readonly ILogger<ProjectService> _logger;
    private readonly AppConfig _config;

    /// <inheritdoc />
    public event EventHandler? ProjectsChanged;

    /// <inheritdoc />
    public string? DefaultLogoPath { get; set; }

    /// <summary>
    ///   Creates the service with the default project logo path.
    /// </summary>
    public ProjectService(
        IConfigService configService,
        ILogoCacheService logoCache,
        ILogger<ProjectService> logger)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = _configService.Load();
        ClearStaleCurrentProject();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetAllProjects()
    {
        RefreshLogos();
        return _config.Projects.ToList();
    }

    /// <inheritdoc />
    public IReadOnlyList<Project> GetRecentProjects(int limit)
    {
        if (limit <= 0)
        {
            return Array.Empty<Project>();
        }

        RefreshLogos();
        return _config.Projects
            .OrderByDescending(p => p.LastUsed)
            .Take(limit)
            .ToList();
    }

    /// <inheritdoc />
    public Project? GetCurrentProject()
    {
        if (_config.CurrentProjectId == null)
        {
            return null;
        }

        var project = _config.Projects.FirstOrDefault(p => p.Id == _config.CurrentProjectId);
        if (project != null)
        {
            project.LogoPath = ResolveProjectLogo(project);
        }

        return project;
    }

    private void RefreshLogos()
    {
        foreach (var project in _config.Projects)
        {
            project.LogoPath = ResolveProjectLogo(project);
        }
    }

    /// <summary>
    ///   Resolves the project logo through the logo cache keyed by the project ID, falling back
    ///   to <see cref="DefaultLogoPath"/> when no logo is found.
    /// </summary>
    private string? ResolveProjectLogo(Project project) =>
        _logoCache.GetOrResolve($"project:{project.Id}", () => ScanLogo(project.Path)) ?? DefaultLogoPath;

    /// <inheritdoc />
    public Project AddProject(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        var normalized = NormalizePath(folderPath);

        if (!Directory.Exists(normalized))
        {
            throw new DirectoryNotFoundException($"Folder does not exist: {normalized}");
        }

        var existing = _config.Projects.FirstOrDefault(p => PathsEqual(p.Path, normalized));
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

        _config.Projects.Add(project);
        _logger.LogInformation("Added project {ProjectName} at {Path}", project.Name, project.Path);
        Persist();
        return project;
    }

    /// <inheritdoc />
    public void RemoveProject(string projectId)
    {
        var project = _config.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        _config.Projects.Remove(project);
        _logoCache.Remove($"project:{projectId}");
        _logger.LogInformation("Removed project {ProjectName}", project.Name);

        if (_config.CurrentProjectId == projectId)
        {
            _config.CurrentProjectId = null;
        }

        Persist();
    }

    /// <inheritdoc />
    public void SetCurrentProject(string projectId)
    {
        var project = _config.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        _config.CurrentProjectId = projectId;
        project.LastUsed = DateTime.UtcNow;
        _logger.LogDebug("Current project set to {ProjectName}", project.Name);
        Persist();
    }

    /// <inheritdoc />
    public void ToggleFavorite(string projectId)
    {
        var project = _config.Projects.FirstOrDefault(p => p.Id == projectId);
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
    internal string? ResolveLogo(string projectPath) => ScanLogo(projectPath) ?? DefaultLogoPath;

    /// <summary>
    ///   Scans the project folder for well-known logo filenames and returns the first match, or
    ///   null when no candidate exists.
    /// </summary>
    private static string? ScanLogo(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return null;
        }

        foreach (var candidate in CandidateLogoNames)
        {
            var candidatePath = Path.Combine(projectPath, candidate);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return null;
    }

    private void ClearStaleCurrentProject()
    {
        if (_config.CurrentProjectId == null)
        {
            return;
        }

        var matches = _config.Projects.Any(p => p.Id == _config.CurrentProjectId);
        if (!matches)
        {
            _logger.LogDebug("Clearing stale current project id {ProjectId}", _config.CurrentProjectId);
            _config.CurrentProjectId = null;
            Persist();
        }
    }

    private void Persist()
    {
        _configService.Save(_config);
        ProjectsChanged?.Invoke(this, EventArgs.Empty);
    }

    internal static string NormalizePath(string path) =>
        Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    internal static bool PathsEqual(string a, string b) =>
        string.Equals(NormalizePath(a), NormalizePath(b), StringComparison.OrdinalIgnoreCase);
}
