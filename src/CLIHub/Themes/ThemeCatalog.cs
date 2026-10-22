namespace CLIHub.Themes;

using CLIHub.Core.Models;

/// <summary>
///   Registry mapping a resolved application theme to its palette dictionary. The palette paths
///   live on <see cref="ThemeDescriptor" />; the dark palette is the base palette merged by
///   App.xaml, and a null resolution keeps that base.
/// </summary>
public static class ThemeCatalog
{
    /// <summary>
    ///   Resolves the palette dictionary to merge for an application theme. Null keeps the base
    ///   palette: explicit Dark, System on a Windows-dark desktop, and every unknown resolution
    ///   stay on the dark base without an extra merge. System follows the Windows mode and never
    ///   resolves to blue.
    /// </summary>
    /// <param name="theme"> The parsed application theme. </param>
    /// <param name="systemPrefersLight"> Whether Windows reports the light apps mode. </param>
    /// <returns> The palette path to merge on top of the base, or null to keep the base. </returns>
    public static string? ResolvePalettePath(AppTheme theme, bool systemPrefersLight)
    {
        var resolvedTheme = theme == AppTheme.System
            ? systemPrefersLight ? AppTheme.Light : AppTheme.Dark
            : theme;

        return ThemeDescriptors.All.FirstOrDefault(descriptor => descriptor.Theme == resolvedTheme)?.PalettePath;
    }
}
