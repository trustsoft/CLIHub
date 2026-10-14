namespace CLIHub.Core.Infrastructure.Persistence;

using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.FileSystem;

/// <summary>
///   Persistent logo cache: resolves logos by a namespaced key, stores outcomes (including
///   negative results) in memory, loads previous state from a JSON file at startup,
///   and saves the state back atomically when disposed at application shutdown.
/// </summary>
public sealed class LogoCacheService : ILogoCacheService
{
    private readonly ILogger<LogoCacheService> logger;
    private readonly ConcurrentDictionary<string, string?> cache = new();
    private readonly string stateFilePath;
    private bool isDirty;
    private bool disposed;

    /// <summary>
    ///   Creates the service, loading previously saved cache state when the state file exists.
    /// </summary>
    /// <param name="logger"> The logger. </param>
    /// <param name="stateFilePath">
    ///   Overrides the state file location; defaults to <c> %APPDATA%\CLIHub\cache\logos.json </c>.
    /// </param>
    public LogoCacheService(ILogger<LogoCacheService> logger, string? stateFilePath = null)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.stateFilePath = stateFilePath ?? AppPaths.LogosCacheFile;

        this.Load();
    }

    /// <inheritdoc />
    public string? GetOrResolve(string key, Func<string?> resolve)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(resolve);

        if (this.cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resolved = resolve();
        if (this.cache.TryAdd(key, resolved))
        {
            this.isDirty = true;
        }

        return resolved;
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (this.cache.TryRemove(key, out _))
        {
            this.isDirty = true;
        }
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        if (this.cache.IsEmpty)
        {
            return;
        }

        this.cache.Clear();
        this.isDirty = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        if (!this.isDirty)
        {
            return;
        }

        try
        {
            this.Save();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not save the logo cache state to {Path}", this.stateFilePath);
        }
    }

    private void Load()
    {
        if (!File.Exists(this.stateFilePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(this.stateFilePath);
            var state = JsonSerializer.Deserialize<CacheState>(json, CoreJson.Options);

            if (state != null)
            {
                foreach (var (key, value) in state.Logos)
                {
                    this.cache[key] = value;
                }
            }

            this.logger.LogDebug("Loaded {Count} logo cache entries from {Path}", this.cache.Count, this.stateFilePath);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Could not read the logo cache state at {Path}; starting with an empty cache", this.stateFilePath);
            this.cache.Clear();
            this.isDirty = true;
        }
    }

    private void Save()
    {
        var state = new CacheState();
        foreach (var (key, value) in this.cache)
        {
            state.Logos[key] = value;
        }

        var json = JsonSerializer.Serialize(state, CoreJson.Options);

        // Atomic write: write to a temp file, then rename, so an interrupted save cannot
        // corrupt the previous state.
        var directory = Path.GetDirectoryName(this.stateFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = this.stateFilePath + ".tmp";
        File.WriteAllText(tempPath, json);
        File.Move(tempPath, this.stateFilePath, overwrite: true);
        this.isDirty = false;
        this.logger.LogDebug("Saved {Count} logo cache entries to {Path}", state.Logos.Count, this.stateFilePath);
    }

    private sealed class CacheState
    {
        /// <summary>
        ///   Cached logo paths keyed by "project:&lt;id&gt;" or "plugin:&lt;id&gt;";
        ///   a null value means the lookup was performed and no logo was found.
        /// </summary>
        public Dictionary<string, string?> Logos { get; set; } = new();
    }
}
