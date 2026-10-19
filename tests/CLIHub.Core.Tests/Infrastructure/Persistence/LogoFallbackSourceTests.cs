namespace CLIHub.Core.Tests.Infrastructure.Persistence;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Persistence;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;

/// <summary>
///   Verifies the single fallback source: both domain resolvers share one
///   <see cref="LogoOptions.DefaultLogoPath" />, and a "not found" outcome stays cached as
///   null instead of freezing the fallback into the cache.
/// </summary>
public class LogoFallbackSourceTests : IDisposable
{
    private const string DefaultLogoPath = "default-logo.png";

    private readonly string tempDir;

    public LogoFallbackSourceTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "clihub-logofallback-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.tempDir, recursive: true); } catch { }
    }

    [Fact]
    public void ProjectAndPluginMisses_SharedOptions_BothFallBackToSameDefault()
    {
        using var cache = new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            Path.Combine(this.tempDir, "logos.json"));
        var options = new LogoOptions(DefaultLogoPath);
        var projectResolver = new ProjectLogoResolver(cache, options);
        var pluginResolver = new PluginLogoResolver(cache, options);

        var emptyFolder = Directory.CreateDirectory(Path.Combine(this.tempDir, "empty")).FullName;
        var project = new Project { Id = "shared-default", Name = "Shared", Path = emptyFolder };

        var projectLogo = projectResolver.Resolve(project);
        var pluginLogo = pluginResolver.Resolve("shared-default", emptyFolder);

        Assert.Equal(DefaultLogoPath, projectLogo);
        Assert.Equal(DefaultLogoPath, pluginLogo);
    }

    [Fact]
    public void ProjectAndPluginMisses_NullStaysCachedInTheStateFile()
    {
        var statePath = Path.Combine(this.tempDir, "logos.json");
        using (var cache = new LogoCacheService(NullLogger<LogoCacheService>.Instance, statePath))
        {
            var options = new LogoOptions(DefaultLogoPath);
            var projectResolver = new ProjectLogoResolver(cache, options);
            var pluginResolver = new PluginLogoResolver(cache, options);

            var emptyFolder = Directory.CreateDirectory(Path.Combine(this.tempDir, "empty")).FullName;
            projectResolver.Resolve(new Project { Id = "fallback", Name = "Fallback", Path = emptyFolder });
            pluginResolver.Resolve("fallback", emptyFolder);
        }

        var state = File.ReadAllText(statePath);

        // The cached outcomes are null (negative caching); the default is applied after the
        // cache lookup, so it never leaks into the persisted state.
        Assert.Contains("\"project:fallback\": null", state);
        Assert.Contains("\"plugin:fallback\": null", state);
        Assert.DoesNotContain(DefaultLogoPath, state);
    }

    [Fact]
    public void ProjectAndPluginMisses_RepeatedLookups_ReuseCachedNegativeOutcome()
    {
        using var cache = new LogoCacheService(
            NullLogger<LogoCacheService>.Instance,
            Path.Combine(this.tempDir, "logos.json"));
        var options = new LogoOptions(DefaultLogoPath);
        var projectResolver = new ProjectLogoResolver(cache, options);
        var pluginResolver = new PluginLogoResolver(cache, options);

        var missingPath = Path.Combine(this.tempDir, "missing");
        var project = new Project { Id = "recheck", Name = "Recheck", Path = missingPath };

        Assert.Equal(DefaultLogoPath, projectResolver.Resolve(project));
        Assert.Equal(DefaultLogoPath, pluginResolver.Resolve("recheck", missingPath));

        // The raw cache holds the negative outcomes: a raw lookup must not re-run a resolver.
        var scans = 0;
        Assert.Null(cache.GetOrResolve(LogoCacheKeys.ProjectPrefix + "recheck", () => { scans++; return null; }));
        Assert.Null(cache.GetOrResolve(LogoCacheKeys.PluginPrefix + "recheck", () => { scans++; return null; }));
        Assert.Equal(0, scans);

        // While the resolvers keep surfacing the default on top of the cached null.
        Assert.Equal(DefaultLogoPath, projectResolver.Resolve(project));
        Assert.Equal(DefaultLogoPath, pluginResolver.Resolve("recheck", missingPath));
    }
}
