namespace CLIHub.Core.Projects;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;

/// <summary>
///   Resolves project logos through the persistent logo cache and filesystem candidates.
/// </summary>
internal sealed class ProjectLogoResolver
{
    private static readonly string[] CandidateLogoNames =
    {
        "logo.png", "logo.jpg", "logo.jpeg", "logo.svg",
        "icon.png", "icon.jpg", "favicon.png"
    };

    private readonly ILogoCacheService _logoCache;

    /// <summary>
    ///   Creates the resolver over the shared logo cache.
    /// </summary>
    /// <param name="logoCache"> The logo cache. </param>
    public ProjectLogoResolver(ILogoCacheService logoCache)
    {
        _logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
    }

    /// <summary>
    ///   Resolves the cached or filesystem logo for a project.
    /// </summary>
    /// <param name="project"> The project whose logo is being resolved. </param>
    /// <param name="defaultLogoPath"> The fallback logo path. </param>
    /// <returns> The resolved logo path, or the fallback path. </returns>
    public string? Resolve(Project project, string? defaultLogoPath) =>
        _logoCache.GetOrResolve($"project:{project.Id}", () => Scan(project.Path)) ?? defaultLogoPath;

    /// <summary>
    ///   Resolves a logo path without using a project cache key.
    /// </summary>
    /// <param name="projectPath"> The project folder to scan. </param>
    /// <param name="defaultLogoPath"> The fallback logo path. </param>
    /// <returns> The first matching logo path, or the fallback path. </returns>
    public string? ResolvePath(string projectPath, string? defaultLogoPath) =>
        Scan(projectPath) ?? defaultLogoPath;

    /// <summary>
    ///   Removes the cached logo for a project.
    /// </summary>
    /// <param name="projectId"> The project identifier. </param>
    public void Remove(string projectId) => _logoCache.Remove($"project:{projectId}");

    private static string? Scan(string projectPath)
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
}
