namespace CLIHub.ViewModels;

using System.Windows.Input;

/// <summary>
///   One entry in a pane's data-driven actions menu: either a separator or an action with an icon,
///   a label, and an optional command.
/// </summary>
public sealed class MenuAction : ObservableObject
{
    private bool _isChecked;

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
    ///   The checked state of a checkable entry.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => SetProperty(ref _isChecked, value);
    }

    /// <summary>
    ///   Creates a separator entry.
    /// </summary>
    /// <returns> A separator menu entry. </returns>
    public static MenuAction Separator() => new() { IsSeparator = true };
}
