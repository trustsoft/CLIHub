namespace CLIHub.Core.Agents;

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   Retrieves an agent's version by running its version command, with per-agent caching
///   that expires after a configurable time-to-live, and a configurable probe timeout.
/// </summary>
public class AgentVersionService : IAgentVersionService
{
    /// <summary>
    ///   The probe time-to-live used when the TTL preference is not set.
    /// </summary>
    public static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    /// <summary>
    ///   The probe timeout used when the timeout preference is not set.
    /// </summary>
    public static readonly TimeSpan DefaultProbeTimeout = TimeSpan.FromSeconds(10);

    private static readonly Regex VersionPattern = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);

    private readonly ProcessLauncher processLauncher;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentVersionService> logger;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<string?>>> inFlight = new();

    /// <summary>
    ///   Creates the service.
    /// </summary>
    public AgentVersionService(ProcessLauncher processLauncher,
                               IPreferencesStore preferencesStore,
                               ILogger<AgentVersionService> logger,
                               TimeProvider? timeProvider = null)
    {
        this.processLauncher = processLauncher;
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
        var result = await this.processLauncher.CaptureOutputAsync(
            command.Executable, command.Arguments, workingDirectory, CancellationToken.None, this.ProbeTimeout);

        string? version = null;
        if (result.Started && result.ExitCode == 0)
        {
            version = ExtractVersion(result.StdOut);
        }
        else
        {
            this.logger.LogWarning("Failed to get version for {PluginId}: {Error}", plugin.Id, result.StdErr);
        }

        this.cache[plugin.Id] = new CacheEntry(version, this.timeProvider.GetUtcNow());
        return version;
    }

    /// <inheritdoc />
    public void Invalidate() => this.cache.Clear();

    private TimeSpan Ttl
    {
        get
        {
            var minutes = this.preferencesStore.Load().AgentProbeTtlMinutes;
            return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl;
        }
    }

    private TimeSpan ProbeTimeout
    {
        get
        {
            var seconds = this.preferencesStore.Load().AgentProbeTimeoutSeconds;
            return seconds is > 0 ? TimeSpan.FromSeconds(seconds.Value) : DefaultProbeTimeout;
        }
    }

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
