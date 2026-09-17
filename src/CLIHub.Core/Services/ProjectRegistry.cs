using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed record ProjectAddResult(bool Success, ProjectConfig? Project, string? Error)
{
    public static ProjectAddResult Ok(ProjectConfig project) => new(true, project, null);

    public static ProjectAddResult Fail(string error) => new(false, null, error);
}

public sealed class ProjectRegistry
{
    private readonly JsonDocumentStore<ProjectsDocument> _store;
    private readonly IFileSystem _fileSystem;

    private ProjectsDocument _document = new();

    public ProjectRegistry(JsonDocumentStore<ProjectsDocument> store, IFileSystem fileSystem)
    {
        _store = store;
        _fileSystem = fileSystem;
        Load();
    }

    public IReadOnlyList<ProjectConfig> Projects => _document.Projects;

    public IReadOnlyList<ProjectConfig> Load()
    {
        _document = _store.Load();
        return _document.Projects;
    }

    public ProjectAddResult Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ProjectAddResult.Fail("Project path is not set.");
        }

        if (!_fileSystem.DirectoryExists(path))
        {
            return ProjectAddResult.Fail($"Folder not found: {path}");
        }

        if (_document.Projects.Any(project => SamePath(project.Path, path)))
        {
            return ProjectAddResult.Fail("A project with this path is already added.");
        }

        var name = DeriveName(path);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProjectAddResult.Fail("Could not determine the project name.");
        }

        var project = new ProjectConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Path = path
        };

        _document.Projects.Add(project);
        _store.Save(_document);
        return ProjectAddResult.Ok(project);
    }

    public bool Remove(string id)
    {
        var project = _document.Projects.FirstOrDefault(candidate => candidate.Id == id);
        if (project is null)
        {
            return false;
        }

        _document.Projects.Remove(project);
        _store.Save(_document);
        return true;
    }

    private static bool SamePath(string? left, string right) =>
        left is not null &&
        string.Equals(Normalize(left), Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) =>
        path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    private static string? DeriveName(string path)
    {
        var trimmed = Normalize(path);
        if (trimmed.Length == 0)
        {
            return null;
        }

        var name = Path.GetFileName(trimmed);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }
}
