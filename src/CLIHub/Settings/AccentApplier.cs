namespace CLIHub.Settings;

using System.Windows;
using System.Windows.Media;

using CLIHub.Core.Models;

/// <summary>
///   Writes the application-scoped accent brushes into the application resources.
/// </summary>
/// <remarks>
///   <para>
///     The accent is one color for the whole application and is independent of the theme: the palette
///     dictionaries define chrome and surface brushes only, so these two keys are supplied here and
///     nowhere else.
///   </para>
///   <para>
///     Both callers pass <see cref="Application.Resources" /> itself, never its
///     <c> MergedDictionaries </c>: local entries are what shadow a merged palette, and the merged
///     collection has no keyed indexer. Startup applies the stored accent before any window is created
///     and a Settings save re-applies it, so the same implementation covers both paths.
///   </para>
///   <para>
///     UI thread only, matching <see cref="CLIHub.Themes.PaletteSwitcher" />; no synchronization is
///     performed.
///   </para>
/// </remarks>
internal static class AccentApplier
{
    /// <summary>
    ///   Normalizes an accent value and writes <c> AccentBrush </c> and
    ///   <c> AccentSelectionBrush </c> into the supplied application resources.
    /// </summary>
    /// <param name="resources">
    ///   The application resources to write into (for example <see cref="Application.Resources" />).
    /// </param>
    /// <param name="accentHex">
    ///   The stored accent; an invalid or missing value falls back to the default accent.
    /// </param>
    public static void Apply(ResourceDictionary resources, string? accentHex) =>
        Apply(resources, accentHex, CreateBrush);

    /// <summary>
    ///   Writes the accent brushes using an alternate brush factory. Test seam; production code uses
    ///   the overload that creates frozen brushes.
    /// </summary>
    /// <param name="resources"> The application resources to write into. </param>
    /// <param name="accentHex"> The stored accent, normalized before use. </param>
    /// <param name="createBrush"> Factory building a brush from an <c> #AARRGGBB </c> value. </param>
    internal static void Apply(
        ResourceDictionary resources,
        string? accentHex,
        Func<string, SolidColorBrush> createBrush)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(createBrush);

        var accent = AccentPalette.Normalize(accentHex);
        resources["AccentBrush"] = createBrush(AccentPalette.Opaque(accent));
        resources["AccentSelectionBrush"] = createBrush(AccentPalette.SelectionTint(accent));
    }

    private static SolidColorBrush CreateBrush(string argbHex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(argbHex));
        brush.Freeze();
        return brush;
    }
}
