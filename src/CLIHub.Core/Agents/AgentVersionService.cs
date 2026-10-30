namespace CLIHub.Core.Agents;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

/// <summary>
///   Retrieves an agent's version by running its version command, with per-agent caching
///   that expires after a configurable time-to-live, and a configurable probe timeout.
/// </summary>
public class AgentVersionService : IAgentVersionService
{
    /// <summary>
    ///   The probe time-to-live used when the TTL preference is not set.
    /// </summary>
    public static TimeSpan DefaultTtl => AgentProbePolicy.DefaultTtl;

    /// <summary>
    ///   The probe timeout used when the timeout preference is not set.
    /// </summary>
    public static TimeSpan DefaultProbeTimeout => AgentProbePolicy.DefaultProbeTimeout;

    private static readonly Regex VersionPattern = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);

    private readonly IProcessOrchestrator orchestrator;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentVersionService> logger;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> inFlight = new();

    /// <summary>
    ///   Creates the service.
    /// </summary>
    public AgentVersionService(
        IProcessOrchestrator orchestrator,
        IPreferencesStore preferencesStore,
        ILogger<AgentVersionService> logger,
        TimeProvider? timeProvider = null)
    {
        this.orchestrator = orchestrator;
        this.preferencesStore = preferencesStore;
        this.logger = logger;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var now = this.timeProvider.GetUtcNow();
        if (this.cache.TryGetValue(plugin.Id, out var cached) && now - cached.Timestamp < this.Ttl)
        {
            return cached.Value;
        }

        var lazy = this.inFlight.GetOrAdd(
            plugin.Id,
            _ => new Lazy<Task<string?>>(
                () => this.ProbeAndCacheAsync(plugin),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                this.inFlight.TryRemove(plugin.Id, out _);
            }
        }
    }

    private async Task<string?> ProbeAndCacheAsync(Plugin plugin)
    {
        var command = plugin.Commands?.Version;
        if (command == null || string.IsNullOrWhiteSpace(command.Executable))
        {
            this.cache[plugin.Id] = new CacheEntry(null, this.timeProvider.GetUtcNow());
            return null;
        }

        var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var runtime = this.preferencesStore.Load().DefaultRuntime;
        var request = LaunchRequest.Create(workingDirectory, command, runtime, CancellationToken.None);
        var result = await this.orchestrator.CaptureOutputAsync(request);

        string? version = null;
        if (result.IsSuccess && result.ExitCode == 0)
        {
            version = ExtractVersion(result.Output ?? string.Empty);
        }
        else
        {
            this.logger.LogWarning("Failed to get version for {PluginId}: {Error}", plugin.Id, result.Error);
        }

        this.cache[plugin.Id] = new CacheEntry(version, this.timeProvider.GetUtcNow());
        return version;
    }

    /// <inheritdoc />
    public void Invalidate() => this.cache.Clear();

    /// <inheritdoc />
    /// <remarks>
    ///   Only a completed cache entry is removed; the in-flight map is deliberately left alone so
    ///   concurrent callers keep sharing one probe. A probe that started before this call may still
    ///   publish its result, which is accepted as at most one probe old.
    /// </remarks>
    public void InvalidateFor(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        this.cache.TryRemove(plugin.Id, out _);
    }

    private TimeSpan Ttl => AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes);

    /// <summary>
    ///   Extracts the version number from command output (for example "1.0.88" from
    ///   "GitHub Copilot CLI 1.0.88."). Falls back to the first non-empty line.
    /// </summary>
    private static string? ExtractVersion(string output)
    {
        var line = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(line))
        {
            return null;
        }

        var match = VersionPattern.Match(line);
        return match.Success ? match.Value : line;
    }

    private readonly record struct CacheEntry(string? Value, DateTimeOffset Timestamp);
}
