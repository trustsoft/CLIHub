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
    void SetTheme(AppTheme theme);
}
