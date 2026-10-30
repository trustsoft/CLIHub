namespace CLIHub.Settings;

using CLIHub.Core.Models;

/// <summary>
///   Applies the application theme palette to the running UI.
/// </summary>
public interface IThemePreferenceTarget
{
    /// <summary>
    ///   Applies the theme palette.
    /// </summary>
    /// <param name="theme"> The theme whose palette becomes the active overlay. </param>
    /// <returns>
    ///   True when the palette was applied or the base palette was intentionally kept; false when the
    ///   palette dictionary could not be loaded or no application resources were available, in which
    ///   case the previous palette is kept.
    /// </returns>
    bool SetTheme(AppTheme theme);
}
