namespace CLIHub.Windows;

using CLIHub.Core.Hotkeys;
using CLIHub.Interop;
using CLIHub.ViewModels;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

/// <summary>
/// The Settings window: dark drawn chrome (no OS title bar) around the preference sections.
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
    /// Reloads the stored preferences and shows (or re-activates) the window. The height matches
    /// the launch window's current height, so the two windows read as the same surface.
    /// </summary>
    public void ShowSettings()
    {
        _viewModel.Load();

        // The launch window stays on top of other windows, so the Settings window has to be owned
        // by it to be visible above it.
        if (Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this))
        {
            Owner = owner;
        }

        if (Application.Current?.MainWindow is { } launch)
        {
            Height = Math.Max(MinHeight, launch.ActualHeight);
        }

        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnSourceInitialized(object? sender, EventArgs e) =>
        DwmApi.TryRoundCorners(new WindowInteropHelper(this).EnsureHandle());

    /// <summary>
    /// Moves the window while the drawn header is dragged. The close button handles the press
    /// itself, so a click on it never starts a drag.
    /// </summary>
    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move started.
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }

    private void HotkeyField_PreviewKeyDown(object sender, KeyEventArgs e)
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
