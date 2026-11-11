namespace CLIHub.Views;

using System.Runtime.InteropServices;

using System.Windows;
using System.Windows.Interop;

using CLIHub.Interop;

/// <summary>
///   Window chrome operations shared across windows: owned-by-launch-window enforcement, show and
///   restore/activate, and DWM rounded corners.
/// </summary>
internal static class WindowChromeHelper
{
    /// <summary>
    ///   Sets the window's owner to the launch window when it is not yet owned and the launch window
    ///   exists. The launch window stays on top of other windows, so owned dialogs must be parented
    ///   to it to be visible above it.
    /// </summary>
    /// <param name="window"> The window to own. </param>
    internal static void EnsureOwnedByLaunchWindow(Window window)
    {
        if (window.Owner is null &&
            Application.Current?.MainWindow is { } owner &&
            !ReferenceEquals(owner, window))
        {
            window.Owner = owner;
        }
    }

    /// <summary>
    ///   Shows the window and activates it. When the window is already visible but minimized,
    ///   restores it to normal state before activating.
    /// </summary>
    /// <param name="window"> The window to show and activate. </param>
    internal static void ShowAndActivate(Window window)
    {
        window.Show();

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    /// <summary>
    ///   Applies DWM rounded corners to the window. Call from <c>SourceInitialized</c>.
    /// </summary>
    /// <param name="window"> The window whose corners should be rounded. </param>
    internal static void ApplyRoundedCorners(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        DwmApi.TryRoundCorners(handle);
    }

    /// <summary>
    ///   Writes <see cref="System.Windows.FrameworkElement.MinWidth" />/
    ///   <see cref="System.Windows.FrameworkElement.MinHeight" /> into the message's
    ///   <see cref="User32.MINMAXINFO" /> as the minimum tracking size, converting layout units
    ///   to physical pixels with the window's DPI because handling the message bypasses WPF's
    ///   own conversion.
    /// </summary>
    /// <param name="window"> The window whose minimum size is enforced. </param>
    /// <param name="hwnd"> The window handle the message was received for. </param>
    /// <param name="lParam"> The message's <see cref="User32.MINMAXINFO" /> pointer. </param>
    internal static void ApplyMinTrackSize(Window window, IntPtr hwnd, IntPtr lParam)
    {
        var dpi = User32.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;

        var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);
        minMax.ptMinTrackSize.X = (int)Math.Ceiling(window.MinWidth * scale);
        minMax.ptMinTrackSize.Y = (int)Math.Ceiling(window.MinHeight * scale);
        Marshal.StructureToPtr(minMax, lParam, false);
    }
}
