using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CLIHub.App.Views;

public sealed class HotkeyCaptureBox : TextBox
{
    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(string),
            typeof(HotkeyCaptureBox),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnHotkeyChanged));

    private bool _capturing;

    public string Hotkey
    {
        get => (string)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    public HotkeyCaptureBox()
    {
        IsReadOnly = true;
        Cursor = Cursors.Hand;
        Text = Hotkey;
        GotKeyboardFocus += OnGotFocus;
        LostKeyboardFocus += OnLostFocus;
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var box = (HotkeyCaptureBox)d;
        if (!box._capturing)
        {
            box.Text = (string)e.NewValue;
        }
    }

    private void OnGotFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _capturing = true;
        Text = "Нажмите сочетание клавиш…";
    }

    private void OnLostFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        _capturing = false;
        Text = Hotkey;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_capturing)
        {
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin)
        {
            e.Handled = true;
            return;
        }

        if (key == Key.Tab && Keyboard.Modifiers == ModifierKeys.None)
        {
            return;
        }

        if (key == Key.Escape)
        {
            _capturing = false;
            Text = Hotkey;
            MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.None)
        {
            Text = "Добавьте Ctrl, Alt, Shift или Win";
            e.Handled = true;
            return;
        }

        if (!TryGetKeyName(key, out var name))
        {
            Text = "Клавиша не поддерживается";
            e.Handled = true;
            return;
        }

        var parts = new List<string>(4);
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(name);

        Hotkey = string.Join("+", parts);
        Text = Hotkey;
        MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        e.Handled = true;
    }

    private static bool TryGetKeyName(Key key, out string name)
    {
        if (key is >= Key.A and <= Key.Z or >= Key.F1 and <= Key.F24)
        {
            name = key.ToString();
            return true;
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            name = ((char)('0' + (key - Key.D0))).ToString();
            return true;
        }

        name = key switch
        {
            Key.Space => "Space",
            Key.Enter => "Enter",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Left => "Left",
            Key.Up => "Up",
            Key.Right => "Right",
            Key.Down => "Down",
            _ => string.Empty
        };

        return name.Length > 0;
    }
}
