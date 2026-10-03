namespace CLIHub.Core.Hotkeys;

/// <summary>
///   Hotkey modifier keys. Values match the Win32 <c>MOD_*</c> constants.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>
    ///   No modifier keys.
    /// </summary>
    None = 0,
    /// <summary>
    ///   The Alt key.
    /// </summary>
    Alt = 1,
    /// <summary>
    ///   The Ctrl key.
    /// </summary>
    Control = 2,
    /// <summary>
    ///   The Shift key.
    /// </summary>
    Shift = 4,
    /// <summary>
    ///   The Windows key.
    /// </summary>
    Win = 8
}
