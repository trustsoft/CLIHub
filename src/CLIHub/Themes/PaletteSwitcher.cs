namespace CLIHub.Themes;

using System;
using System.Collections.Generic;
using System.Windows;

using Serilog;

using CLIHub.Core.Models;

/// <summary>
///   Owns the single palette overlay merged into the application resources on top of the dark base
///   palette declared by App.xaml, and swaps or removes it when the theme changes.
/// </summary>
/// <remarks>
///   <para>
///     UI thread only. Startup applies the palette from <c>App.OnStartup</c> and a Settings save
///     applies it from the dispatcher, so no synchronization is performed.
///   </para>
///   <para>
///     Exactly one overlay is tracked. The dark palette stays the base merged by App.xaml; a null
///     resolution (explicit Dark, System on a Windows-dark desktop, unknown values) removes the
///     tracked overlay and keeps that base. When the overlay is still present it is replaced in
///     place, so the merge order of the other dictionaries is preserved.
///   </para>
///   <para>
///     A palette that cannot be loaded never produces a half-applied theme: the candidate dictionary
///     is built before the collection is touched, the failure is logged as a warning, and the
///     previously applied palette stays in effect.
///   </para>
/// </remarks>
public sealed class PaletteSwitcher
{
    private readonly Func<string, ResourceDictionary> createPalette;
    private ResourceDictionary? overlay;

    /// <summary>
    ///   Creates a switcher that loads palettes from the component resources of this assembly.
    /// </summary>
    internal PaletteSwitcher()
        : this(CreatePalette)
    {
    }

    /// <summary>
    ///   Creates a switcher with an alternate palette factory. Test seam; production code uses the
    ///   parameterless constructor.
    /// </summary>
    /// <param name="createPalette"> Factory building the dictionary for a resolved palette path. </param>
    internal PaletteSwitcher(Func<string, ResourceDictionary> createPalette) => this.createPalette = createPalette;

    /// <summary>
    ///   The single switcher shared by startup and the DI-resolved preference target. Both must
    ///   track the same overlay, otherwise the first switch would leak the startup palette.
    /// </summary>
    public static PaletteSwitcher Shared { get; } = new();

    /// <summary>
    ///   Applies the palette registered for a theme, replacing or removing the tracked overlay.
    /// </summary>
    /// <param name="merged"> The merged dictionaries of the application resources. </param>
    /// <param name="theme"> The theme whose palette becomes active. </param>
    /// <param name="systemPrefersLight"> Whether Windows reports the light apps mode. </param>
    /// <returns>
    ///   True when the palette was applied or the base palette was kept; false when the palette
    ///   dictionary could not be loaded, in which case <paramref name="merged" /> is unchanged.
    /// </returns>
    public bool Apply(IList<ResourceDictionary> merged, AppTheme theme, bool systemPrefersLight)
    {
        ArgumentNullException.ThrowIfNull(merged);

        ResourceDictionary? palette = null;
        var path = ThemeCatalog.ResolvePalettePath(theme, systemPrefersLight);

        if (path is not null)
        {
            try
            {
                palette = this.createPalette(path);
            }
            catch (Exception ex)
            {
                Log.Warning(
                    ex,
                    "Could not load the {Theme} palette from {PalettePath}; the current palette was kept",
                    theme,
                    path);
                return false;
            }
        }

        if (palette is null)
        {
            if (this.overlay is not null)
            {
                merged.Remove(this.overlay);
                this.overlay = null;
            }

            return true;
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
