using CLIHub.Core.Hotkeys;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace CLIHub.Hotkeys;

/// <summary>
/// Registers a global hotkey against a window and invokes a callback when it is pressed.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xC1A0;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly ILogger<GlobalHotkeyService> _logger;
    private readonly HwndSource _source;
    private readonly IntPtr _handle;
    private readonly Action _onPressed;
    private bool _registered;

    public bool IsRegistered => _registered;

    public GlobalHotkeyService(Window window, Action onPressed, ILogger<GlobalHotkeyService> logger)
    {
        _logger = logger;
        _onPressed = onPressed;

        _handle = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(_handle)
                  ?? throw new InvalidOperationException("Could not obtain an HwndSource for the window.");
        _source.AddHook(WndProc);
    }

    /// <summary>
    /// Registers the hotkey. Returns false (and logs) when the combination is unavailable.
    /// </summary>
    public bool Register(HotkeyDefinition definition)
    {
        if (_registered)
        {
            return true;
        }

        _registered = RegisterHotKey(_handle, HotkeyId, (uint)definition.Modifiers, (uint)definition.VirtualKey);

        if (_registered)
        {
            _logger.LogInformation(
                "Registered global hotkey (modifiers {Modifiers}, vk 0x{VirtualKey:X2})",
                definition.Modifiers, definition.VirtualKey);
        }
        else
        {
            _logger.LogWarning(
                "Failed to register global hotkey (modifiers {Modifiers}, vk 0x{VirtualKey:X2}); it may be in use",
                definition.Modifiers, definition.VirtualKey);
        }

        return _registered;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _onPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }

        _source.RemoveHook(WndProc);
    }
}
