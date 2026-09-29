namespace CLIHub.Windows;

using CLIHub.Core.Hotkeys;
using CLIHub.ViewModels;
using System.Windows;
using System.Windows.Input;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
    }

    /// <summary>
    /// Reloads the stored preferences and shows (or re-activates) the window.
    /// </summary>
    public void ShowSettings()
    {
        _viewModel.Load();
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

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
            _viewModel.SetHotkeyError("Hotkey must include at least one modifier (Ctrl, Shift, Alt, Win).");
            return;
        }

        var definition = new HotkeyDefinition(modifiers, KeyInterop.VirtualKeyFromKey(key));
        if (!HotkeyParser.TryParse(HotkeyParser.Format(definition), out _))
        {
            _viewModel.SetHotkeyError("Unsupported key. Use a letter, digit, F1-F24, or a named key (Enter, Tab, Escape, arrows, and so on) with a modifier.");
            return;
        }

        _viewModel.SetCapturedHotkey(definition);
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
