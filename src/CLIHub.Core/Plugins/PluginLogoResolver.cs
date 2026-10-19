namespace CLIHub.Core.Plugins;

using CLIHub.Core.Infrastructure.Persistence;

/// <summary>
///   Resolves plugin logos through the persistent logo cache, falling back to the shared
///   default logo exactly once, at resolver level.
/// </summary>
public sealed class PluginLogoResolver
{
    private const string CandidateLogoName = "logo.png";

    private readonly ILogoCacheService logoCache;
    private readonly LogoOptions options;

    /// <summary>
    ///   Creates the resolver over the shared logo cache and fallback options.
    /// </summary>
    /// <param name="logoCache"> The logo cache. </param>
    /// <param name="options"> The shared logo options supplying the default logo path. </param>
    public PluginLogoResolver(ILogoCacheService logoCache, LogoOptions options)
    {
        this.logoCache = logoCache ?? throw new ArgumentNullException(nameof(logoCache));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    ///   Resolves the cached or filesystem logo for a plugin. The resolved value is final:
    ///   when no logo exists the negative outcome stays cached as null and the default logo
    ///   path is returned instead.
    /// </summary>
    /// <param name="pluginId"> The plugin identifier used as the cache key. </param>
    /// <param name="pluginDirectory"> The plugin folder to scan. </param>
    /// <returns> The resolved logo path, or the default logo path. </returns>
    public string? Resolve(string pluginId, string pluginDirectory) =>
        this.logoCache.GetOrResolve(LogoCacheKeys.PluginPrefix + pluginId, () => Scan(pluginDirectory))
        ?? this.options.DefaultLogoPath;

    private static string? Scan(string pluginDirectory)
    {
        if (string.IsNullOrWhiteSpace(pluginDirectory))
        {
            return null;
        }

        var logoPath = Path.Combine(pluginDirectory, CandidateLogoName);

        return File.Exists(logoPath) ? logoPath : null;
    }
}
