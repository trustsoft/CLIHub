namespace CLIHub.Core.Infrastructure.Persistence;

/// <summary>
///   The single home of the logo-cache key namespace: every cache key is a domain prefix
///   plus the domain identity, and invalidation scopes by the same prefixes.
/// </summary>
public static class LogoCacheKeys
{
    /// <summary>
    ///   Key prefix for project logo entries; the rest of the key is the project ID.
    /// </summary>
    public const string ProjectPrefix = "project:";

    /// <summary>
    ///   Key prefix for plugin logo entries; the rest of the key is the plugin ID.
    /// </summary>
    public const string PluginPrefix = "plugin:";
}
