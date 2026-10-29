namespace CLIHub.Hotkeys;

using System.Windows;
using System.Windows.Interop;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Hotkeys;
using CLIHub.Interop;

/// <summary>
///   Registers a global hotkey against a window and invokes a callback when it is pressed.
/// </summary>
public sealed class GlobalHotkeyService : IGlobalHotkeyService, IDisposable
{
    private const int HotkeyId = 0xC1A0;

    private readonly ILogger<GlobalHotkeyService> logger;
    private readonly HwndSource source;
    private readonly IntPtr handle;
    private readonly Action onPressed;
    private bool registered;
    private HotkeyDefinition? current;

    /// <summary>
    ///   Hooks the window's message loop so hotkey messages can be observed.
    /// </summary>
    /// <param name="window"> The window that receives <c> WM_HOTKEY </c>. </param>
    /// <param name="onPressed"> Callback invoked when the hotkey is pressed. </param>
    /// <param name="logger"> Logger. </param>
    public GlobalHotkeyService(Window window, Action onPressed, ILogger<GlobalHotkeyService> logger)
    {
        this.logger = logger;
        this.onPressed = onPressed;

        this.handle = new WindowInteropHelper(window).EnsureHandle();
        this.source = HwndSource.FromHwnd(this.handle)
                  ?? throw new InvalidOperationException("Could not obtain an HwndSource for the window.");
        this.source.AddHook(this.WndProc);
    }

    /// <summary>
    ///   Registers the hotkey. Returns false (and logs) when the combination is unavailable.
    /// </summary>
    public bool Register(HotkeyDefinition definition)
    {
        if (this.registered)
        {
            return true;
        }

        this.registered = User32.RegisterHotKey(this.handle, HotkeyId, (uint)definition.Modifiers, (uint)definition.VirtualKey);

        if (this.registered)
        {
            this.current = definition;
            this.logger.LogInformation(
                "Registered global hotkey (modifiers {Modifiers}, vk 0x{VirtualKey:X2})",
                definition.Modifiers, definition.VirtualKey);
        }
        else
        {
            this.logger.LogWarning(
                "Failed to register global hotkey (modifiers {Modifiers}, vk 0x{VirtualKey:X2}); it may be in use",
                definition.Modifiers, definition.VirtualKey);
        }

        return this.registered;
    }

    /// <summary>
    ///   Swaps the registered hotkey for a new combination. If the new combination cannot
    ///   be registered (for example it is already in use), the previous combination is
    ///   restored and false is returned.
    /// </summary>
    public bool ReRegister(HotkeyDefinition definition)
    {
        var previous = this.current;

        if (this.registered)
        {
            User32.UnregisterHotKey(this.handle, HotkeyId);
            this.registered = false;
        }

        if (this.Register(definition))
        {
            return true;
        }

        if (previous is not null)
        {
            this.Register(previous);
        }

        return false;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == User32.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            this.onPressed();
            handled = true;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    ///   Unregisters the hotkey and unhooks from the window's message loop.
    /// </summary>
    public void Dispose()
    {
        if (this.registered)
        {
            User32.UnregisterHotKey(this.handle, HotkeyId);
            this.registered = false;
        }

        this.source.RemoveHook(this.WndProc);
    }
}
