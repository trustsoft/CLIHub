namespace CLIHub.Core.Services;

using CLIHub.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
///   Persistent logo cache: resolves logos by a namespaced key, stores outcomes (including
///   negative results) in memory, loads previous state from a JSON file at startup, and saves
///   the state back atomically when disposed at application shutdown.
/// </summary>
public sealed class LogoCacheService : ILogoCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ILogger<LogoCacheService> _logger;
    private readonly ConcurrentDictionary<string, string?> _cache = new();
    private readonly string _stateFilePath;
    private bool _isDirty;
    private bool _disposed;

    /// <summary>
    ///   Creates the service, loading previously saved cache state when the state file exists.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="stateFilePath"> Overrides the state file location; defaults to <c>%APPDATA%\CLIHub\cache\logos.json</c>. </param>
    public LogoCacheService(ILogger<LogoCacheService> logger, string? stateFilePath = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _stateFilePath = stateFilePath ?? AppPaths.LogosCacheFile;

        Load();
    }

    /// <inheritdoc />
    public string? GetOrResolve(string key, Func<string?> resolve)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(resolve);

        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resolved = resolve();
        if (_cache.TryAdd(key, resolved))
        {
            _isDirty = true;
        }

        return resolved;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (_cache.TryRemove(key, out _))
        {
            _isDirty = true;
        }
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        if (_cache.IsEmpty)
        {
            return;
        }

        _cache.Clear();
        _isDirty = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (!_isDirty)
        {
            return;
        }

        try
        {
            Save();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save the logo cache state to {Path}", _stateFilePath);
        }
    }

    private void Load()
    {
        if (!File.Exists(_stateFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(_stateFilePath);
            var state = JsonSerializer.Deserialize<CacheState>(json, JsonOptions);

            if (state != null)
            {
                foreach (var (key, value) in state.Logos)
                {
                    _cache[key] = value;
                }
            }

            _logger.LogDebug("Loaded {Count} logo cache entries from {Path}", _cache.Count, _stateFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read the logo cache state at {Path}; starting with an empty cache", _stateFilePath);
            _cache.Clear();
            _isDirty = true;
        }
    }

    private void Save()
    {
        var state = new CacheState();
        foreach (var (key, value) in _cache)
        {
            state.Logos[key] = value;
        }

        var json = JsonSerializer.Serialize(state, JsonOptions);

        // Atomic write: write to a temp file, then rename, so an interrupted save cannot
        // corrupt the previous state.
        var directory = Path.GetDirectoryName(_stateFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = _stateFilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, _stateFilePath, overwrite: true);
        _isDirty = false;
        _logger.LogDebug("Saved {Count} logo cache entries to {Path}", state.Logos.Count, _stateFilePath);
    }

    private sealed class CacheState
    {
        /// <summary>
        ///   Cached logo paths keyed by "project:&lt;id&gt;" or "plugin:&lt;id&gt;"; a null value
        ///   means the lookup was performed and no logo was found.
        /// </summary>
        public Dictionary<string, string?> Logos { get; set; } = new();
    }
}
