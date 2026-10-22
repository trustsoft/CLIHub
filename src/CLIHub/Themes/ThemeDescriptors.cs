namespace CLIHub.Themes;

using CLIHub.Core.Models;

/// <summary>
///   Per-theme presentation data: the picker text, the palette dictionary to merge, and the preview
///   swatch resource keys shown on the theme card.
/// </summary>
/// <param name="Theme"> The application theme the descriptor describes. </param>
/// <param name="Name"> Display name shown in the theme picker. </param>
/// <param name="Description"> One-line explanation shown under the name in the theme picker. </param>
/// <param name="PalettePath">
///   The palette dictionary to merge on top of the dark base, or null to keep the base palette.
/// </param>
/// <param name="CardBrush"> Resource key of the preview card background brush. </param>
/// <param name="BarBrush"> Resource key of the preview bar brush. </param>
public sealed record ThemeDescriptor(
    AppTheme Theme,
    string Name,
    string Description,
    string? PalettePath,
    string CardBrush,
    string BarBrush);

/// <summary>
///   The single source of truth for per-theme data in the WPF layer. Theme descriptors are listed in
///   picker order; the preview brush keys resolve to the fixed swatches declared in
///   <c> ThemePickerData.xaml </c>.
/// </summary>
/// <remarks>
///   The preview dot is deliberately not part of the descriptor: every real theme previews the live
///   accent brush, and only <see cref="AppTheme.System" /> swaps it for the muted swatch. The view
///   model carries that as a flag so the accent dot keeps tracking accent changes.
/// </remarks>
public static class ThemeDescriptors
{
    /// <summary>
    ///   The theme descriptors in picker order: Dark, Light, Blue, System.
    /// </summary>
    /// <remarks>
    ///   System mirrors the dark base for its palette (the palette choice follows the Windows mode) and
    ///   previews its two-tone card with the light bar.
    /// </remarks>
    public static IReadOnlyList<ThemeDescriptor> All { get; } =
    [
        new ThemeDescriptor(
            AppTheme.Dark,
            "Dark Theme",
            "Fluent Dark (default)",
            null,
            "ThemePreviewDarkBrush",
            "ThemePreviewBarDarkBrush"),
        new ThemeDescriptor(
            AppTheme.Light,
            "Light Theme",
            "High contrast daylight profile",
            "/Themes/LightTheme.xaml",
            "ThemePreviewLightBrush",
            "ThemePreviewBarLightBrush"),
        new ThemeDescriptor(
            AppTheme.Blue,
            "Blue Theme",
            "Blue-tinted dark chrome with a baked-in blue accent",
            "/Themes/BlueTheme.xaml",
            "ThemePreviewBlueBrush",
            "ThemePreviewBarBlueBrush"),
        new ThemeDescriptor(
            AppTheme.System,
            "System Sync",
            "Follow Windows personalization state",
            null,
            "ThemePreviewSystemBrush",
            "ThemePreviewBarLightBrush")
    ];
}
