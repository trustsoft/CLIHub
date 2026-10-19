namespace CLIHub.Core.Projects;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;

/// <summary>
///   Resolves project logos through the persistent logo cache and filesystem candidates,
///   falling back to the shared default logo exactly once, at resolver level.
/// </summary>
public sealed class ProjectLogoResolver
{
    private static readonly string[] CandidateLogoNames =
    {
        "logo.png", "logo.jpg", "logo.jpeg", "logo.svg",
        "icon.png", "icon.jpg", "favicon.png"
    };

    private readonly ILogoCacheService logoCache;
    private readonly LogoOptions options;

    /// <summary>
    ///   Creates the resolver over the shared logo cache and fallback options.
    /// </summary>
    /// <param name="logoCache"> The logo cache. </param>
    /// <param name="options"> The shared logo options supplying the default logo path. </param>
    public ProjectLogoResolver(ILogoCacheService logoCache, LogoOptions options)
    {
        this.logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///   Resolves the cached or filesystem logo for a project.
    /// </summary>
    /// <param name="project"> The project whose logo is being resolved. </param>
    /// <returns> The resolved logo path, or the default logo path. </returns>
    public string? Resolve(Project project) =>
        this.logoCache.GetOrResolve(LogoCacheKeys.ProjectPrefix + project.Id, () => Scan(project.Path))
        ?? this.options.DefaultLogoPath;

    /// <summary>
    ///   Resolves a logo path without using a project cache key.
    /// </summary>
    /// <param name="projectPath"> The project folder to scan. </param>
    /// <returns> The first matching logo path, or the default logo path. </returns>
    public string? ResolvePath(string projectPath) => Scan(projectPath) ?? this.options.DefaultLogoPath;

    /// <summary>
    ///   Removes the cached logo for a project.
    /// </summary>
    /// <param name="projectId"> The project identifier. </param>
    public void Remove(string projectId) => this.logoCache.Remove(LogoCacheKeys.ProjectPrefix + projectId);

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
