namespace CLIHub.Core.Hotkeys;

/// <summary>
///   A parsed global hotkey: modifier flags plus a Win32 virtual-key code.
/// </summary>
/// <param name="Modifiers"> Modifier keys; must not be <see cref="HotkeyModifiers.None"/>. </param>
/// <param name="VirtualKey"> Win32 virtual-key code (for example 0x41 for 'A'). </param>
public sealed record HotkeyDefinition(HotkeyModifiers Modifiers, int VirtualKey);
