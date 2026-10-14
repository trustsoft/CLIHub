namespace CLIHub.Core.Scanning;

using CLIHub.Core.Models;
using CLIHub.Core.Projects;

/// <summary>
///   Walks watched roots and decides which directories are repositories.
///   Directory-only: this never starts a process and never executes plugin code.
/// </summary>
public static class RepositoryDiscovery
{
    private static readonly string[] NotificationOnlyIndicators = ["openspec"];

    /// <summary>
    ///   Scans the watched roots and returns the discovered repositories.
    /// </summary>
    /// <param name="options"> The effective scanning options. </param>
    /// <param name="plugins"> The loaded plugins whose project indicators participate in discovery. </param>
    /// <param name="onRootError"> Optional callback invoked when a root cannot be scanned. </param>
    /// <returns> The discovery result for this pass. </returns>
    public static DiscoveryResult Enumerate(
        ProjectScanningOptions options,
        IReadOnlyList<Plugin> plugins,
        Action<string, Exception>? onRootError = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        var indicators = CollectAgentIndicators(plugins);
        var discovered = new List<string>();
        var scannedRoots = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in options.WatchedDirectories)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                continue;
            }

            string normalizedRoot;
            try
            {
                normalizedRoot = ProjectPathPolicy.Normalize(root);
            }
            catch (Exception ex)
            {
                onRootError?.Invoke(root, ex);
                continue;
            }

            if (!Directory.Exists(normalizedRoot))
            {
                onRootError?.Invoke(root, new DirectoryNotFoundException($"Watched directory does not exist: {root}"));
                continue;
            }

            try
            {
                Walk(normalizedRoot, 0, options, indicators, discovered, seen);
                scannedRoots.Add(normalizedRoot);
            }
            catch (Exception ex)
            {
                onRootError?.Invoke(root, ex);
            }
        }

        return new DiscoveryResult(discovered, scannedRoots);
    }

    private static void Walk(
        string directory,
        int depth,
        ProjectScanningOptions options,
        IReadOnlyList<string> indicators,
        List<string> discovered,
        HashSet<string> seen)
    {
        if (IsRepository(directory, options.DiscoveryMode, indicators))
        {
            if (seen.Add(directory))
            {
                discovered.Add(directory);
            }

            return;
        }

        if (depth >= options.ScanDepth)
        {
            return;
        }

        string[] children;
        try
        {
            children = Directory.GetDirectories(directory);
        }
        catch (Exception)
        {
            return;
        }

        foreach (var child in children)
        {
            var name = Path.GetFileName(child);

            if (string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (IgnoredPatternMatcher.IsIgnored(name, options.IgnoredPatterns))
            {
                continue;
            }

            Walk(child, depth + 1, options, indicators, discovered, seen);
        }
    }

    private static bool IsRepository(string directory, ProjectDiscoveryMode mode, IReadOnlyList<string> indicators)
    {
        if (mode is ProjectDiscoveryMode.Git or ProjectDiscoveryMode.Both)
        {
            var git = Path.Combine(directory, ".git");
            if (Directory.Exists(git) || File.Exists(git))
            {
                return true;
            }
        }

        if (mode is ProjectDiscoveryMode.Agents or ProjectDiscoveryMode.Both)
        {
            foreach (var indicator in indicators)
            {
                var combined = Path.Combine(directory, indicator);
                if (Directory.Exists(combined) || File.Exists(combined))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static IReadOnlyList<string> CollectAgentIndicators(IReadOnlyList<Plugin> plugins)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in plugins)
        {
            foreach (var indicator in plugin.Detection?.ProjectIndicators ?? Enumerable.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(indicator))
                {
                    continue;
                }

                var trimmed = indicator.Trim();
                if (NotificationOnlyIndicators.Any(excluded => string.Equals(excluded, trimmed, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (seen.Add(trimmed))
                {
                    result.Add(trimmed);
                }
            }
        }

        return result;
    }
}
