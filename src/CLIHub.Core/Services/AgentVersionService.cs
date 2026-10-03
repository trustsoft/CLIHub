namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

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

    private readonly IProcessLauncher _processLauncher;
    private readonly IConfigService _configService;
    private readonly ILogger<AgentVersionService> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();

    /// <summary>
    ///   Creates the service.
    /// </summary>
    public AgentVersionService(
        IProcessLauncher processLauncher,
        IConfigService configService,
        ILogger<AgentVersionService> logger,
        TimeProvider? timeProvider = null)
    {
        _processLauncher = processLauncher;
        _configService = configService;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        if (_cache.TryGetValue(plugin.Id, out var cached) && now - cached.Timestamp < Ttl)
        {
            return cached.Value;
        }

        var command = plugin.Commands?.Version;
        if (command == null || string.IsNullOrWhiteSpace(command.Executable))
        {
            _cache[plugin.Id] = new CacheEntry(null, now);
            return null;
        }

        var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = await _processLauncher.CaptureOutputAsync(
            command.Executable, command.Arguments, workingDirectory, cancellationToken, ProbeTimeout);

        string? version = null;
        if (result.Started && result.ExitCode == 0)
        {
            version = ExtractVersion(result.StdOut);
        }
        else
        {
            _logger.LogWarning("Failed to get version for {PluginId}: {Error}", plugin.Id, result.StdErr);
        }

        _cache[plugin.Id] = new CacheEntry(version, now);
        return version;
    }

    /// <inheritdoc />
    public void Invalidate() => _cache.Clear();

    private TimeSpan Ttl
    {
        get
        {
            var minutes = _configService.Load().Preferences.AgentProbeTtlMinutes;
            return minutes is > 0 ? TimeSpan.FromMinutes(minutes.Value) : DefaultTtl;
        }
    }

    private TimeSpan ProbeTimeout
    {
        get
        {
            var seconds = _configService.Load().Preferences.AgentProbeTimeoutSeconds;
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
