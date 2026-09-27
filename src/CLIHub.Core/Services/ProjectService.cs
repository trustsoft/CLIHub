namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Tracks project directories backed by <see cref="IConfigService"/>.
/// </summary>
public class ProjectService : IProjectService
{
    private static readonly string[] CandidateLogoNames =
    {
        "logo.png", "logo.jpg", "logo.jpeg", "logo.svg",
        "icon.png", "icon.jpg", "favicon.png"
    };

    private readonly IConfigService _configService;
    private readonly ILogger<ProjectService> _logger;
    private readonly AppConfig _config;

    public event EventHandler? ProjectsChanged;

    public string? DefaultLogoPath { get; set; }

    public ProjectService(IConfigService configService, ILogger<ProjectService> logger)
    {
        _configService = configService ?? throw new ArgumentNullException(nameof(configService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _config = _configService.Load();
        ClearStaleCurrentProject();
    }

    public IReadOnlyList<Project> GetAllProjects()
    {
        RefreshLogos();
        return _config.Projects.ToList();
    }

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

    public IReadOnlyList<Project> GetFavorites()
    {
        RefreshLogos();
        return _config.Projects.Where(p => p.IsFavorite).ToList();
    }

    public Project? GetCurrentProject()
    {
        if (_config.CurrentProjectId == null)
        {
            return null;
        }

        var project = _config.Projects.FirstOrDefault(p => p.Id == _config.CurrentProjectId);
        if (project != null)
        {
            project.LogoPath = ResolveLogo(project.Path);
        }

        return project;
    }

    private void RefreshLogos()
    {
        foreach (var project in _config.Projects)
        {
            project.LogoPath = ResolveLogo(project.Path);
        }
    }

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
            LastUsed = DateTime.UtcNow,
            LogoPath = ResolveLogo(normalized)
        };

        if (string.IsNullOrEmpty(project.Name))
        {
            project.Name = normalized;
        }

        _config.Projects.Add(project);
        _logger.LogInformation("Added project {ProjectName} at {Path}", project.Name, project.Path);
        Persist();
        return project;
    }

    public void RemoveProject(string projectId)
    {
        var project = _config.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        _config.Projects.Remove(project);
        _logger.LogInformation("Removed project {ProjectName}", project.Name);

        if (_config.CurrentProjectId == projectId)
        {
            _config.CurrentProjectId = null;
        }

        Persist();
    }

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

    public void TouchProject(string projectId)
    {
        var project = _config.Projects.FirstOrDefault(p => p.Id == projectId);
        if (project == null)
        {
            return;
        }

        project.LastUsed = DateTime.UtcNow;
        Persist();
    }

    public string? ResolveLogo(string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath))
        {
            return DefaultLogoPath;
        }

        foreach (var candidate in CandidateLogoNames)
        {
            var candidatePath = Path.Combine(projectPath, candidate);
            if (File.Exists(candidatePath))
            {
                return candidatePath;
            }
        }

        return DefaultLogoPath;
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
