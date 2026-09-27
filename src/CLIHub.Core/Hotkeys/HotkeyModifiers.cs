namespace CLIHub.Core.Hotkeys;

/// <summary>
/// Hotkey modifier keys. Values match the Win32 <c>MOD_*</c> constants.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
