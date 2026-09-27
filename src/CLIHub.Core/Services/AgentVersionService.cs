namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

/// <summary>
/// Retrieves an agent's version by running its version command, with per-agent caching.
/// </summary>
public class AgentVersionService : IAgentVersionService
{
    private static readonly Regex VersionPattern = new(@"\d+(?:\.\d+)+", RegexOptions.Compiled);

    private readonly IProcessLauncher _processLauncher;
    private readonly ILogger<AgentVersionService> _logger;
    private readonly ConcurrentDictionary<string, string?> _cache = new();

    public AgentVersionService(IProcessLauncher processLauncher, ILogger<AgentVersionService> logger)
    {
        _processLauncher = processLauncher;
        _logger = logger;
    }

    public async Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(plugin.Id, out var cached))
        {
            return cached;
        }

        var command = plugin.Commands?.Version;
        if (command == null || string.IsNullOrWhiteSpace(command.Executable))
        {
            _cache[plugin.Id] = null;
            return null;
        }

        var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var result = await _processLauncher.CaptureOutputAsync(
            command.Executable, command.Arguments, workingDirectory, cancellationToken);

        string? version = null;
        if (result.Started && result.ExitCode == 0)
        {
            version = ExtractVersion(result.StdOut);
        }
        else
        {
            _logger.LogWarning("Failed to get version for {PluginId}: {Error}", plugin.Id, result.StdErr);
        }

        _cache[plugin.Id] = version;
        return version;
    }

    public void Invalidate() => _cache.Clear();

    /// <summary>
    /// Extracts the version number from command output (for example "1.0.88" from
    /// "GitHub Copilot CLI 1.0.88."). Falls back to the first non-empty line.
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
}
