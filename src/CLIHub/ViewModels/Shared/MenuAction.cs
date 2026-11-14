namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   One entry in a pane's data-driven actions menu: either a separator
///   or an action with an icon, a label, and an optional command.
/// </summary>
public sealed class MenuAction : ObservableObject
{
    /// <summary>
    ///   The entry's label; empty for a separator.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    ///   The icon glyph shown before the label.
    /// </summary>
    public string Glyph { get; init; } = string.Empty;

    /// <summary>
    ///   The command run when the entry is invoked; null for separators and toggles.
    /// </summary>
    public ICommand? Command { get; init; }

    /// <summary>
    ///   Whether the entry renders as a toggle with a checked state.
    /// </summary>
    public bool IsCheckable { get; init; }

    /// <summary>
    ///   Whether the entry is a separator rather than an action.
    /// </summary>
    public bool IsSeparator { get; init; }

    /// <summary>
    ///   Optional tooltip text for the menu entry.
    /// </summary>
    public string? Tooltip { get; init; }

    /// <summary>
    ///   Optional accessible name for the menu entry; falls back to Label when null or empty.
    /// </summary>
    public string? AccessibleName { get; init; }

    /// <summary>
    ///   The effective accessible name for the menu entry; returns AccessibleName if provided, otherwise Label.
    /// </summary>
    public string EffectiveAccessibleName => string.IsNullOrEmpty(this.AccessibleName) ? this.Label : this.AccessibleName;

    /// <summary>
    ///   The checked state of a checkable entry.
    /// </summary>
    public bool IsChecked
    {
        get;
        set => this.SetProperty(ref field, value);
    }

    /// <summary>
    ///   Creates a separator entry.
    /// </summary>
    /// <returns> A separator menu entry. </returns>
    public static MenuAction Separator() => new() { IsSeparator = true };
}
