namespace CLIHub;

using CLIHub.Core.Models;

/// <summary>
///   Applies the display style used for project paths.
/// </summary>
public interface IPathDisplayStyleTarget
{
    /// <summary>
    ///   Applies a path display style and raises the target's normal state notifications.
    /// </summary>
    /// <param name="style"> Style to apply. </param>
    void ApplyPathDisplayStyle(PathDisplayStyle style);
}
