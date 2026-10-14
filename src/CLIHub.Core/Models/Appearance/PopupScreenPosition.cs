namespace CLIHub.Core.Models;

/// <summary>
///   Where the launch window is placed when it is shown.
/// </summary>
public enum PopupScreenPosition
{
    /// <summary>
    ///   Centred in the work area of the monitor that contains the pointer.
    /// </summary>
    Center,

    /// <summary>
    ///   Horizontally centred near the top of the pointer's monitor work area.
    /// </summary>
    TopCenter,

    /// <summary>
    ///   Near the pointer, clamped into the pointer's monitor work area.
    /// </summary>
    CursorRelative,

    /// <summary>
    ///   Centred in the primary monitor work area.
    /// </summary>
    PrimaryScreen
}

/// <summary>
///   Maps <see cref="PopupScreenPosition" /> to and from its stored token.
/// </summary>
public static class PopupScreenPositions
{
    /// <summary>
    ///   The token for the active-monitor center position.
    /// </summary>
    public const string CenterToken = "center";

    /// <summary>
    ///   The token for the active-monitor top-center position.
    /// </summary>
    public const string TopCenterToken = "topCenter";

    /// <summary>
    ///   The token for the cursor-relative position.
    /// </summary>
    public const string CursorRelativeToken = "cursorRelative";

    /// <summary>
    ///   The token for the primary-screen position.
    /// </summary>
    public const string PrimaryToken = "primary";

    /// <summary>
    ///   Parses a stored token. Returns false for null, empty, or unknown values.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <param name="position"> The parsed position when recognized. </param>
    /// <returns> True when the token is recognized. </returns>
    public static bool TryParse(string? value, out PopupScreenPosition position)
    {
        position = PopupScreenPosition.Center;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        switch (value.Trim())
        {
            case "center":
            case "centre":
            case "center-of-active-monitor":
                position = PopupScreenPosition.Center;
                return true;
            case "topCenter":
            case "top-center":
            case "top":
                position = PopupScreenPosition.TopCenter;
                return true;
            case "cursorRelative":
            case "cursor-relative":
            case "cursor":
                position = PopupScreenPosition.CursorRelative;
                return true;
            case "primary":
            case "primary-screen":
                position = PopupScreenPosition.PrimaryScreen;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    ///   Parses a stored token, falling back to <see cref="PopupScreenPosition.Center" />.
    /// </summary>
    /// <param name="value"> The stored token. </param>
    /// <returns> The parsed or default position. </returns>
    public static PopupScreenPosition Parse(string? value) =>
        TryParse(value, out var position) ? position : PopupScreenPosition.Center;

    /// <summary>
    ///   Returns the stored token for a position.
    /// </summary>
    /// <param name="position"> The position. </param>
    /// <returns> The canonical token. </returns>
    public static string ToToken(PopupScreenPosition position) => position switch
    {
        PopupScreenPosition.TopCenter => TopCenterToken,
        PopupScreenPosition.CursorRelative => CursorRelativeToken,
        PopupScreenPosition.PrimaryScreen => PrimaryToken,
        _ => CenterToken
    };
}
