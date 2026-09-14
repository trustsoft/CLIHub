using System.Windows.Interop;

namespace CLIHub.App.Interop;

public sealed class HotkeyManager : IDisposable
{
    private const int WmHotkey = 0x0312;
    private const int HotkeyId = 0x4C48;

    private HwndSource? _source;

    public event Action? Pressed;

    public bool TryRegister(string hotkey, out string? error)
    {
        error = null;
        EnsureSource();

        if (!HotkeyParser.TryParse(hotkey, out var modifiers, out var virtualKey, out error))
        {
            return false;
        }

        if (!NativeMethods.RegisterHotKey(_source!.Handle, HotkeyId, modifiers, virtualKey))
        {
            error = $"Комбинация '{hotkey}' уже используется другим приложением.";
            return false;
        }

        return true;
    }

    private void EnsureSource()
    {
        if (_source is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("CLIHubHotkeyWindow")
        {
            Width = 0,
            Height = 0,
            PositionX = 0,
            PositionY = 0,
            WindowStyle = 0,
            ExtendedWindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            Pressed?.Invoke();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_source is null)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(_source.Handle, HotkeyId);
        _source.RemoveHook(WndProc);
        _source.Dispose();
        _source = null;
    }
}
