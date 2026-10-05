namespace CLIHub.Core.Infrastructure.Persistence;

using System;

/// <summary>
///   Caches logo resolution results keyed by a stable identity, so repeated lookups avoid
///   file-system scans and resolved values persist across application runs.
/// </summary>
public interface ILogoCacheService : IDisposable
{
    /// <summary>
    ///   Returns the cached logo path for the key without touching the file system. On a miss,
    ///   invokes the resolver, stores its outcome (including a null "not found" result) in the
    ///   cache, and returns it.
    /// </summary>
    /// <param name="key"> The unique cache key, for example "project:&lt;id&gt;" or "plugin:&lt;id&gt;". </param>
    /// <param name="resolve"> The resolver invoked when the key is not cached. </param>
    /// <returns> The cached or freshly resolved logo path, or null when no logo was found. </returns>
    string? GetOrResolve(string key, Func<string?> resolve);

    /// <summary>
    ///   Removes the cache entry for the key, so the next lookup re-resolves. Has no effect
    ///   when the key is not cached.
    /// </summary>
    /// <param name="key"> The unique cache key to remove. </param>
    void Remove(string key);

    /// <summary>
    ///   Clears all cached entries so the next lookup re-resolves from the file system.
    /// </summary>
    void InvalidateAll();
}
