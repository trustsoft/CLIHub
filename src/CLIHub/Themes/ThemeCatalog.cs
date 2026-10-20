namespace CLIHub.Themes;

using CLIHub.Core.Models;

/// <summary>
///   Registry mapping a resolved application theme to its palette dictionary. The dark palette is
///   the base palette merged by App.xaml; a null resolution keeps that base.
/// </summary>
public static class ThemeCatalog
{
    /// <summary>
    ///   The application-relative palette path of the dark theme, which App.xaml merges as the base
    ///   palette.
    /// </summary>
    public const string DarkPalettePath = "/Themes/DarkTheme.xaml";

    /// <summary>
    ///   Resolves the palette dictionary to merge for an application theme. Null keeps the base
    ///   palette: explicit Dark, System on a Windows-dark desktop, and every unknown resolution
    ///   stay on the dark base without an extra merge. System never resolves to blue.
    /// </summary>
    /// <param name="theme"> The parsed application theme. </param>
    /// <param name="systemPrefersLight"> Whether Windows reports the light apps mode. </param>
    /// <returns> The palette path to merge on top of the base, or null to keep the base. </returns>
    public static string? ResolvePalettePath(AppTheme theme, bool systemPrefersLight) => theme switch
    {
        AppTheme.Light => "/Themes/LightTheme.xaml",
        AppTheme.Blue => "/Themes/BlueTheme.xaml",
        AppTheme.System => systemPrefersLight ? "/Themes/LightTheme.xaml" : null,
        _ => null
    };
}
