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
    private readonly ConfigStore _store;
    private readonly IFileSystem _fileSystem;

    private Config _config = new();

    public ProjectRegistry(ConfigStore store, IFileSystem fileSystem)
    {
        _store = store;
        _fileSystem = fileSystem;
        Load();
    }

    public IReadOnlyList<ProjectConfig> Projects => _config.Projects;

    public string? Runtime => _config.Runtime;

    public string Hotkey => _config.Hotkey;

    public IReadOnlyList<ProjectConfig> Load()
    {
        _config = _store.Load();
        return _config.Projects;
    }

    public ProjectAddResult Add(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return ProjectAddResult.Fail("Путь проекта не задан.");
        }

        if (!_fileSystem.DirectoryExists(path))
        {
            return ProjectAddResult.Fail($"Папка не найдена: {path}");
        }

        if (_config.Projects.Any(project => SamePath(project.Path, path)))
        {
            return ProjectAddResult.Fail("Проект с таким путём уже добавлен.");
        }

        var name = DeriveName(path);
        if (string.IsNullOrWhiteSpace(name))
        {
            return ProjectAddResult.Fail("Не удалось определить имя проекта.");
        }

        var project = new ProjectConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = name,
            Path = path
        };

        _config.Projects.Add(project);
        _store.Save(_config);
        return ProjectAddResult.Ok(project);
    }

    public bool Remove(string id)
    {
        var project = _config.Projects.FirstOrDefault(candidate => candidate.Id == id);
        if (project is null)
        {
            return false;
        }

        _config.Projects.Remove(project);
        _store.Save(_config);
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
