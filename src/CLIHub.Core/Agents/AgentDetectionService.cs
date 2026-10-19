namespace CLIHub.Core.Agents;

using System.Collections.Concurrent;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

/// <summary>
///   Detects agent availability using only file-system checks, caching results for a
///   configurable time-to-live.
/// </summary>
public class AgentDetectionService : IAgentDetectionService
{
    internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    private readonly IPreferencesStore preferencesStore;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();

    /// <summary>
    ///   Creates the service with an optional time source for tests.
    /// </summary>
    public AgentDetectionService(IPreferencesStore preferencesStore, TimeProvider? timeProvider = null)
    {
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public bool IsInstalledInSystem(Plugin plugin) =>
        this.GetOrCheck(BuildSystemKey(plugin.Id), () => CheckSystem(plugin));

    /// <inheritdoc />
    public bool IsAvailableInProject(Plugin plugin, string projectPath) =>
        this.GetOrCheck(BuildProjectKey(plugin.Id, projectPath), () => CheckProject(plugin, projectPath));

    /// <inheritdoc />
    public void Invalidate() => this.cache.Clear();

    /// <inheritdoc />
    public void InvalidateFor(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        this.cache.TryRemove(BuildSystemKey(plugin.Id), out _);

        var projectPrefix = BuildProjectKeyPrefix(plugin.Id);
        foreach (var key in this.cache.Keys)
        {
            if (key.StartsWith(projectPrefix, StringComparison.Ordinal))
            {
                this.cache.TryRemove(key, out _);
            }
        }
    }

    private static string BuildSystemKey(string pluginId) => $"sys:{pluginId}";

    private static string BuildProjectKeyPrefix(string pluginId) => $"proj:{pluginId}:";

    private static string BuildProjectKey(string pluginId, string projectPath) =>
        $"{BuildProjectKeyPrefix(pluginId)}{projectPath}";

    private bool GetOrCheck(string key, Func<bool> compute)
    {
        var now = this.timeProvider.GetUtcNow();
        if (this.cache.TryGetValue(key, out var entry) && now - entry.Timestamp < this.Ttl)
        {
            return entry.Value;
        }

        var value = compute();
        this.cache[key] = new CacheEntry(value, now);
        return value;
    }

    private TimeSpan Ttl
    {
        get
        {
            var minutes = this.preferencesStore.Load().AgentProbeTtlMinutes;
            return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl;
        }
    }

    private static bool CheckSystem(Plugin plugin)
    {
        foreach (var path in plugin.Detection?.SystemPaths ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var expanded = Environment.ExpandEnvironmentVariables(path);
            if (File.Exists(expanded) || Directory.Exists(expanded))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CheckProject(Plugin plugin, string projectPath)
    {
        if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
        {
            return false;
        }

        foreach (var indicator in plugin.Detection?.ProjectIndicators ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(indicator))
            {
                continue;
            }

            var combined = Path.Combine(projectPath, indicator);
            if (File.Exists(combined) || Directory.Exists(combined))
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct CacheEntry(bool Value, DateTimeOffset Timestamp);
}
