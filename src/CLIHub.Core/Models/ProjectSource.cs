namespace CLIHub.Core.Models;

/// <summary>
///   How a project entered CLIHub's project list.
/// </summary>
public enum ProjectSource
{
    /// <summary>
    ///   Registered by the user by selecting a folder.
    /// </summary>
    Manual,

    /// <summary>
    ///   Discovered automatically by repository scanning.
    /// </summary>
    Discovered
}

/// <summary>
///   Parses and formats the <see cref="ProjectSource"/> value stored in configuration.
/// </summary>
public static class ProjectSources
{
    /// <summary>
    ///   Configuration token for <see cref="ProjectSource.Manual"/>.
    /// </summary>
    public const string ManualToken = "manual";

    /// <summary>
    ///   Configuration token for <see cref="ProjectSource.Discovered"/>.
    /// </summary>
    public const string DiscoveredToken = "discovered";

    /// <summary>
    ///   Returns the source for a stored token, falling back to <see cref="ProjectSource.Manual"/>
    ///   for missing or unrecognized values.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <returns> The parsed source. </returns>
    public static ProjectSource Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        DiscoveredToken => ProjectSource.Discovered,
        _ => ProjectSource.Manual
    };

    /// <summary>
    ///   Returns the configuration token for a source.
    /// </summary>
    /// <param name="source"> The source. </param>
    /// <returns> The stored token. </returns>
    public static string ToToken(ProjectSource source) =>
        source == ProjectSource.Discovered ? DiscoveredToken : ManualToken;
}
