namespace CLIHub.Theming;

using System;
using System.Collections.Generic;
using System.Windows;

using Serilog;

/// <summary>
///   Owns the single system-menu palette overlay merged into the application resources and swaps
///   it when the Windows apps light/dark mode changes. The palette follows the Windows apps theme
///   only — it is independent of the stored application theme and of the applied CLIHub palette.
/// </summary>
/// <remarks>
/// <para>
///   UI thread only. Startup applies the menu palette from <c> App.OnStartup </c> (after the accent
///   and CLIHub palette) and a Windows mode flip applies it from the dispatcher through the
///   <c> SystemMenuThemeFollower </c>, so no synchronization is performed.
/// </para>
/// <para>
///   Exactly one overlay is tracked; there is no null resolution because dark is a file, not an
///   absence of one. When the overlay is still present it is replaced in place, so the merge order
///   of the other dictionaries is preserved.
/// </para>
/// <para>
///   A palette that cannot be loaded never produces a half-applied menu theme: the candidate
///   dictionary is built before the collection is touched, the failure is logged as a warning, and
///   the previously applied palette stays in effect.
/// </para>
/// </remarks>
public sealed class SystemMenuTheme
{
    private const string LightPalettePath = "/Themes/SystemMenu/SystemMenuLight.xaml";
    private const string DarkPalettePath = "/Themes/SystemMenu/SystemMenuDark.xaml";

    private readonly Func<string, ResourceDictionary> createPalette;
    private ResourceDictionary? overlay;

    /// <summary>
    ///   Creates a switcher that loads system-menu palettes from the component resources of this
    ///   assembly.
    /// </summary>
    internal SystemMenuTheme()
        : this(CreatePalette)
    {
    }

    /// <summary>
    ///   Creates a switcher with an alternate palette factory. Test seam; production code uses the
    ///   parameterless constructor.
    /// </summary>
    /// <param name="createPalette"> Factory building the dictionary for a resolved palette path. </param>
    internal SystemMenuTheme(Func<string, ResourceDictionary> createPalette) => this.createPalette = createPalette;

    /// <summary>
    ///   The single switcher shared by startup and the menu-mode follower. Both must track the same
    ///   overlay, otherwise the first swap would leak the startup menu palette.
    /// </summary>
    public static SystemMenuTheme Shared { get; } = new();

    /// <summary>
    ///   Applies the system-menu palette matching the Windows apps mode, replacing the tracked
    ///   overlay in place or appending it on first use.
    /// </summary>
    /// <param name="merged"> The merged dictionaries of the application resources. </param>
    /// <param name="systemPrefersLight"> Whether Windows reports the light apps mode. </param>
    /// <returns>
    ///   True when the palette was applied; false when the palette dictionary could not be loaded,
    ///   in which case <paramref name="merged" /> is unchanged.
    /// </returns>
    public bool Apply(IList<ResourceDictionary> merged, bool systemPrefersLight)
    {
        ArgumentNullException.ThrowIfNull(merged);

        var path = systemPrefersLight ? LightPalettePath : DarkPalettePath;

        ResourceDictionary palette;
        try
        {
            palette = this.createPalette(path);
        }
        catch (Exception ex)
        {
            Log.Warning(
                ex,
                "Could not load the {AppsMode} system-menu palette from {PalettePath}; the current menu palette was kept",
                systemPrefersLight ? "light" : "dark",
                path);
            return false;
        }

        var index = this.overlay is null ? -1 : merged.IndexOf(this.overlay);

        if (index >= 0)
        {
            merged[index] = palette;
        }
        else
        {
            merged.Add(palette);
        }

        this.overlay = palette;
        return true;
    }

    private static ResourceDictionary CreatePalette(string path) =>
        new()
        {
            Source = new Uri($"pack://application:,,,/CLIHub;component{path}", UriKind.Absolute)
        };
}