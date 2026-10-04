namespace CLIHub.Core.Services;

using System.Collections.Concurrent;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
///   Detects agent availability using only file-system checks, caching results for a
///   configurable time-to-live.
/// </summary>
public class AgentDetectionService : IAgentDetectionService
{
    internal static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    private readonly IPreferencesStore _preferencesStore;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    ///   Creates the service with an optional time source for tests.
    /// </summary>
    public AgentDetectionService(IPreferencesStore preferencesStore, TimeProvider? timeProvider = null)
    {
        _preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    ///   Creates the service over the legacy configuration contract during migration.
    /// </summary>
    /// <param name="configService"> The shared configuration service. </param>
    /// <param name="timeProvider"> The time source used for cache expiry. </param>
    internal AgentDetectionService(IConfigService configService, TimeProvider? timeProvider = null)
        : this(new PreferencesStore(configService), timeProvider)
    {
    }

    /// <inheritdoc />
    public bool IsInstalledInSystem(Plugin plugin) =>
        GetOrCheck($"sys:{plugin.Id}", () => CheckSystem(plugin));

    /// <inheritdoc />
    public bool IsAvailableInProject(Plugin plugin, string projectPath) =>
        GetOrCheck($"proj:{plugin.Id}:{projectPath}", () => CheckProject(plugin, projectPath));

    /// <inheritdoc />
    public void Invalidate() => _cache.Clear();

    private bool GetOrCheck(string key, Func<bool> compute)
    {
        var now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(key, out var entry) && now - entry.Timestamp < Ttl)
        {
            return entry.Value;
        }

        var value = compute();
        _cache[key] = new CacheEntry(value, now);
        return value;
    }

    private TimeSpan Ttl
    {
        get
        {
            var minutes = _preferencesStore.Load().AgentProbeTtlMinutes;
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
