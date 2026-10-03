namespace CLIHub.Hotkeys;

using CLIHub.Core.Hotkeys;
using CLIHub.Interop;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Interop;

/// <summary>
///   Registers a global hotkey against a window and invokes a callback when it is pressed.
/// </summary>
public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0xC1A0;

    private readonly ILogger<GlobalHotkeyService> _logger;
    private readonly HwndSource _source;
    private readonly IntPtr _handle;
    private readonly Action _onPressed;
    private bool _registered;
    private HotkeyDefinition? _current;

    /// <summary>
    ///   True when a hotkey is currently registered.
    /// </summary>
    public bool IsRegistered => _registered;

    /// <summary>
    ///   Hooks the window's message loop so hotkey messages can be observed.
    /// </summary>
    /// <param name="window"> The window that receives <c>WM_HOTKEY</c>. </param>
    /// <param name="onPressed"> Callback invoked when the hotkey is pressed. </param>
    /// <param name="logger"> Logger. </param>
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
    ///   Registers the hotkey. Returns false (and logs) when the combination is unavailable.
    /// </summary>
    public bool Register(HotkeyDefinition definition)
    {
        if (_registered)
        {
            return true;
        }

        _registered = User32.RegisterHotKey(_handle, HotkeyId, (uint)definition.Modifiers, (uint)definition.VirtualKey);

        if (_registered)
        {
            _current = definition;
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

    /// <summary>
    ///   Swaps the registered hotkey for a new combination. If the new combination cannot
    ///   be registered (for example it is already in use), the previous combination is
    ///   restored and false is returned.
    /// </summary>
    public bool ReRegister(HotkeyDefinition definition)
    {
        var previous = _current;

        if (_registered)
        {
            User32.UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }

        if (Register(definition))
        {
            return true;
        }

        if (previous is not null)
        {
            Register(previous);
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            _onPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///   Unregisters the hotkey and unhooks from the window's message loop.
    /// </summary>
    public void Dispose()
    {
        if (_registered)
        {
            User32.UnregisterHotKey(_handle, HotkeyId);
            _registered = false;
        }

        _source.RemoveHook(WndProc);
    }
}
