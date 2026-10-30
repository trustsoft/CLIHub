namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   A selectable default runtime with a friendly label and an icon.
/// </summary>
/// <param name="Kind"> The runtime kind. </param>
/// <param name="Name"> Display name shown in the combo box and diagnostics. </param>
/// <param name="Icon"> Pack URI of the icon rendered beside the name. </param>
/// <param name="IsInstalled">
///   Whether the runtime executable was found. An unavailable option stays selectable so the stored
///   selection remains visible and re-selectable.
/// </param>
public sealed record RuntimeOption(RuntimeKind Kind, string Name, string Icon, bool IsInstalled = true)
{
    /// <summary>
    ///   The label shown in the selector; unavailable runtimes are marked as not installed.
    /// </summary>
    public string DisplayName => this.IsInstalled ? this.Name : $"{this.Name} (not installed)";
}

/// <summary>
///   A selectable path display style with a friendly label.
/// </summary>
/// <param name="Style"> The path display style. </param>
/// <param name="Name"> Display name shown in the combo box and tooltips. </param>
public sealed record PathDisplayOption(PathDisplayStyle Style, string Name);

/// <summary>
///   A selectable repository scan depth.
/// </summary>
/// <param name="Value"> The depth in directory levels. </param>
/// <param name="Name"> Display name. </param>
public sealed record ScanDepthOption(int Value, string Name);

/// <summary>
///   A selectable log level.
/// </summary>
/// <param name="Value"> The stored level name. </param>
/// <param name="Name"> Display name. </param>
public sealed record LogLevelOption(string Value, string Name);

/// <summary>
///   A selectable popup screen position.
/// </summary>
/// <param name="Position"> The position. </param>
/// <param name="Name"> Display name. </param>
public sealed record PopupPositionOption(PopupScreenPosition Position, string Name);

/// <summary>
///   A selectable application theme.
/// </summary>
/// <param name="Theme"> The theme. </param>
/// <param name="Name"> Display name. </param>
/// <param name="Description"> One-line explanation shown under the name in the theme picker. </param>
/// <param name="PreviewCardBrush"> Resource key of the preview card background brush. </param>
/// <param name="PreviewBarBrush"> Resource key of the preview bar brush. </param>
/// <param name="UsesAccentPreviewDot">
///   Whether the preview dot follows the live accent brush instead of the fixed muted swatch.
/// </param>
public sealed record AppThemeOption(
    AppTheme Theme,
    string Name,
    string Description,
    string PreviewCardBrush,
    string PreviewBarBrush,
    bool UsesAccentPreviewDot);

/// <summary>
///   A selectable accent color: a palette preset or the custom hex entry.
/// </summary>
/// <param name="Name"> Display name. </param>
/// <param name="Hex"> The swatch color, or <c> Transparent </c> for the custom entry. </param>
/// <param name="Hint"> The muted line under the name: the hex value or the custom-field hint. </param>
/// <param name="IsCustom"> True for the custom hex entry. </param>
public sealed record AccentChoice(string Name, string Hex, string Hint, bool IsCustom)
{
    /// <summary>
    ///   The accessible name combining the label with the color value or hint.
    /// </summary>
    public string AccessibleName => $"{this.Name} {this.Hint}";
}

/// <summary>
///   A selectable repository discovery mode.
/// </summary>
/// <param name="Mode"> The discovery mode. </param>
/// <param name="Name"> Display name. </param>
public sealed record DiscoveryModeOption(ProjectDiscoveryMode Mode, string Name);
