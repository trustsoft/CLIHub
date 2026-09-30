namespace CLIHub.Core.Models;

/// <summary>
/// How long project paths are shortened for display in the launch window.
/// </summary>
public enum PathDisplayStyle
{
    /// <summary>Drop the beginning so the end of the path (the folder name) stays visible.</summary>
    LeftTrim,

    /// <summary>Shorten the middle so that both the beginning and the end stay visible.</summary>
    MiddleEllipsis
}

/// <summary>
/// Parses and formats the <see cref="PathDisplayStyle"/> value stored in configuration.
/// </summary>
public static class PathDisplayStyles
{
    /// <summary>Configuration token for <see cref="PathDisplayStyle.LeftTrim"/>.</summary>
    public const string LeftTrimToken = "leftTrim";

    /// <summary>Configuration token for <see cref="PathDisplayStyle.MiddleEllipsis"/>.</summary>
    public const string MiddleEllipsisToken = "middleEllipsis";

    /// <summary>The style used when none is stored or the stored value is unrecognized.</summary>
    public const PathDisplayStyle Default = PathDisplayStyle.LeftTrim;

    /// <summary>
    /// Returns the style for a stored token, falling back to <see cref="Default"/> for missing,
    /// empty or unrecognized values.
    /// </summary>
    public static PathDisplayStyle Parse(string? value) => value?.Trim() switch
    {
        null or "" => Default,
        var token when token.Equals(MiddleEllipsisToken, StringComparison.OrdinalIgnoreCase)
            || token.Equals("middle-ellipsis", StringComparison.OrdinalIgnoreCase)
            || token.Equals("middle", StringComparison.OrdinalIgnoreCase) => PathDisplayStyle.MiddleEllipsis,
        var token when token.Equals(LeftTrimToken, StringComparison.OrdinalIgnoreCase)
            || token.Equals("left-trim", StringComparison.OrdinalIgnoreCase)
            || token.Equals("left", StringComparison.OrdinalIgnoreCase) => PathDisplayStyle.LeftTrim,
        _ => Default
    };

    /// <summary>Returns the configuration token for a style.</summary>
    public static string ToToken(PathDisplayStyle style) => style switch
    {
        PathDisplayStyle.MiddleEllipsis => MiddleEllipsisToken,
        _ => LeftTrimToken
    };
}
