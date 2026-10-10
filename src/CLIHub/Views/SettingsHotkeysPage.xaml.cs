namespace CLIHub.Views;

using System.Windows.Controls;
using System.Windows.Input;

using CLIHub.Core.Hotkeys;
using CLIHub.ViewModels;

/// <summary>
///   Settings page for the global hotkey: capture field with key chips.
/// </summary>
public partial class SettingsHotkeysPage : UserControl
{
    /// <summary>
    ///   Creates the page.
    /// </summary>
    public SettingsHotkeysPage() => InitializeComponent();

    /// <summary>
    ///   A Border does not take keyboard focus on click the way a Control does, so the click
    ///   handler has to move focus here — otherwise the PreviewKeyDown capture never runs.
    /// </summary>
    private void HotkeyField_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HotkeyField.Focus();
        e.Handled = true;
    }

    private void HotkeyField_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        if (DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            // Cancel capture; the field keeps its current value.
            return;
        }

        if (key == Key.None || IsModifierKey(key))
        {
            return;
        }

        var modifiers = ToHotkeyModifiers(Keyboard.Modifiers);
        if (modifiers == HotkeyModifiers.None)
        {
            viewModel.SetHotkeyError("Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win).");
            return;
        }

        var definition = new HotkeyDefinition(modifiers, KeyInterop.VirtualKeyFromKey(key));
        if (!HotkeyParser.TryParse(HotkeyParser.Format(definition), out _))
        {
            viewModel.SetHotkeyError("Unsupported key. Use a letter, digit, F1-F24, or a named key (Enter, Tab, Escape, arrows, and so on) with a modifier.");
            return;
        }

        viewModel.SetCapturedHotkey(definition);
    }

    private static bool IsModifierKey(Key key) => key is
        Key.LeftCtrl or Key.RightCtrl or
        Key.LeftShift or Key.RightShift or
        Key.LeftAlt or Key.RightAlt or
        Key.LWin or Key.RWin or
        Key.System;

    private static HotkeyModifiers ToHotkeyModifiers(ModifierKeys modifiers)
    {
        var result = HotkeyModifiers.None;

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            result |= HotkeyModifiers.Control;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            result |= HotkeyModifiers.Shift;
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            result |= HotkeyModifiers.Alt;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            result |= HotkeyModifiers.Win;
        }

        return result;
    }
}
