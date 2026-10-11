namespace CLIHub.Views;

using System.Windows;
using System.Windows.Controls;

/// <summary>
///   Where a setting card places its control.
/// </summary>
public enum SettingCardPlacement
{
    /// <summary>
    ///   Compact control aligned on the trailing edge of the card.
    /// </summary>
    Right,

    /// <summary>
    ///   Wide control shown below the title and hint, spanning the card width.
    /// </summary>
    Stacked
}

/// <summary>
///   The tone of an optional inline status badge on a setting card.
/// </summary>
public enum SettingBadgeTone
{
    /// <summary> Neutral (muted) badge. </summary>
    Neutral,

    /// <summary> Accent badge. </summary>
    Accent,

    /// <summary> Success (green) badge. </summary>
    Success
}

/// <summary>
///   A card that presents one setting: an optional leading icon, a title, an optional inline
///   badge, and a hint, with the setting's control shown inline.
/// </summary>
public sealed class SettingCard : ContentControl
{
    /// <summary>
    ///   Identifies the <see cref="Title"/> property.
    /// </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Hint"/> property.
    /// </summary>
    public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
        nameof(Hint), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Icon"/> property.
    /// </summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="Badge"/> property.
    /// </summary>
    public static readonly DependencyProperty BadgeProperty = DependencyProperty.Register(
        nameof(Badge), typeof(string), typeof(SettingCard), new PropertyMetadata(string.Empty));

    /// <summary>
    ///   Identifies the <see cref="BadgeTone"/> property.
    /// </summary>
    public static readonly DependencyProperty BadgeToneProperty = DependencyProperty.Register(
        nameof(BadgeTone), typeof(SettingBadgeTone), typeof(SettingCard), new PropertyMetadata(SettingBadgeTone.Neutral));

    /// <summary>
    ///   Identifies the <see cref="Placement"/> property.
    /// </summary>
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(
        nameof(Placement), typeof(SettingCardPlacement), typeof(SettingCard), new PropertyMetadata(SettingCardPlacement.Right));

    /// <summary>
    ///   Identifies the <see cref="Grouped"/> property.
    /// </summary>
    public static readonly DependencyProperty GroupedProperty = DependencyProperty.Register(
        nameof(Grouped), typeof(bool), typeof(SettingCard), new PropertyMetadata(false));

    /// <summary>
    ///   The setting title.
    /// </summary>
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    ///   The setting hint shown under the title.
    /// </summary>
    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    /// <summary>
    ///   An optional leading Segoe MDL2 glyph; empty hides the icon block.
    /// </summary>
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>
    ///   An optional inline status badge next to the title; empty hides the badge.
    /// </summary>
    public string Badge
    {
        get => (string)GetValue(BadgeProperty);
        set => SetValue(BadgeProperty, value);
    }

    /// <summary>
    ///   The badge tone.
    /// </summary>
    public SettingBadgeTone BadgeTone
    {
        get => (SettingBadgeTone)GetValue(BadgeToneProperty);
        set => SetValue(BadgeToneProperty, value);
    }

    /// <summary>
    ///   Where the control is placed.
    /// </summary>
    public SettingCardPlacement Placement
    {
        get => (SettingCardPlacement)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>
    ///   When true, the card draws no surface and is a row inside a <see cref="SettingsGroup"/>.
    /// </summary>
    public bool Grouped
    {
        get => (bool)GetValue(GroupedProperty);
        set => SetValue(GroupedProperty, value);
    }
}
