using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public sealed class LogoResolver
{
    private static readonly string[] Chain =
    {
        "logo.png",
        "logo.ico",
        "icon.png",
        "icon.ico",
        "favicon.ico"
    };

    private readonly IFileSystem _fileSystem;

    public LogoResolver(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public string? Resolve(string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }

        foreach (var candidate in Chain)
        {
            var path = Path.Combine(root, candidate);
            if (_fileSystem.FileExists(path))
            {
                return path;
            }
        }

        return null;
    }

    public string? ResolveProject(ProjectConfig project)
    {
        var root = project.Path;

        var overridePath = ResolveOverride(project.Logo, root);
        if (overridePath is not null)
        {
            return overridePath;
        }

        return Resolve(root);
    }

    private string? ResolveOverride(string? logo, string? root)
    {
        if (string.IsNullOrWhiteSpace(logo))
        {
            return null;
        }

        var path = Path.IsPathRooted(logo) ? logo : Path.Combine(root ?? string.Empty, logo);
        path = Path.GetFullPath(path);
        return _fileSystem.FileExists(path) ? path : null;
    }
}
