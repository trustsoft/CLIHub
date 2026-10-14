namespace CLIHub.Core.Models;

/// <summary>
///   Which directories count as repositories during project scanning.
/// </summary>
public enum ProjectDiscoveryMode
{
    /// <summary>
    ///   Only directories that contain a <c> .git </c> entry.
    /// </summary>
    Git,

    /// <summary>
    ///   Only directories that contain a plugin project indicator (for example <c> .claude </c>).
    /// </summary>
    Agents,

    /// <summary>
    ///   Either of the above. The default.
    /// </summary>
    Both
}

/// <summary>
///   Parses and formats the <see cref="ProjectDiscoveryMode" /> value stored in configuration.
/// </summary>
public static class ProjectDiscoveryModes
{
    /// <summary>
    ///   Configuration token for <see cref="ProjectDiscoveryMode.Git" />.
    /// </summary>
    public const string GitToken = "git";

    /// <summary>
    ///   Configuration token for <see cref="ProjectDiscoveryMode.Agents" />.
    /// </summary>
    public const string AgentsToken = "agents";

    /// <summary>
    ///   Configuration token for <see cref="ProjectDiscoveryMode.Both" />.
    /// </summary>
    public const string BothToken = "both";

    /// <summary>
    ///   The mode used when none is stored or the stored value is unrecognized.
    /// </summary>
    public const ProjectDiscoveryMode Default = ProjectDiscoveryMode.Both;

    /// <summary>
    ///   Returns the mode for a stored token, falling back to <see cref="Default" /> for missing
    ///   or unrecognized values.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <returns> The parsed mode. </returns>
    public static ProjectDiscoveryMode Parse(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        GitToken => ProjectDiscoveryMode.Git,
        AgentsToken => ProjectDiscoveryMode.Agents,
        BothToken => ProjectDiscoveryMode.Both,
        _ => Default
    };

    /// <summary>
    ///   Returns the configuration token for a mode.
    /// </summary>
    /// <param name="mode"> The mode. </param>
    /// <returns> The stored token. </returns>
    public static string ToToken(ProjectDiscoveryMode mode) => mode switch
    {
        ProjectDiscoveryMode.Git => GitToken,
        ProjectDiscoveryMode.Agents => AgentsToken,
        _ => BothToken
    };
}
