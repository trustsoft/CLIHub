namespace CLIHub.Interop;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

/// <summary>
/// Places the launch window on the monitor that currently contains the pointer, without
/// depending on Windows Forms.
/// </summary>
internal static class WindowPositioner
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const double MinimumMargin = 8d;
    private const double MinimumHeight = 240d;

    /// <summary>
    /// Returns the work area of the monitor containing the pointer, in device-independent units.
    /// </summary>
    internal static Rect GetPointerMonitorWorkArea(Window window)
    {
        var scale = GetDeviceToDiuScale(window);

        if (TryGetPointerWorkArea(out var work))
        {
            return new Rect(
                work.Left * scale.X,
                work.Top * scale.Y,
                (work.Right - work.Left) * scale.X,
                (work.Bottom - work.Top) * scale.Y);
        }

        return SystemParameters.WorkArea;
    }

    /// <summary>
    /// Centres the window in the pointer's monitor work area and caps its height to that area.
    /// The move itself is done in native screen coordinates: the window and the monitor work area
    /// are both measured by the operating system, so the result stays correct when the monitors
    /// use different scale factors.
    /// </summary>
    internal static void PlaceOnPointerMonitor(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();

        ApplyHeightCap(window);

        if (!TryGetPointerWorkArea(out var work) || !User32.GetWindowRect(handle, out var current))
        {
            PlaceUsingLayout(window);
            return;
        }

        var width = current.Right - current.Left;
        var height = current.Bottom - current.Top;

        if (width <= 0 || height <= 0)
        {
            PlaceUsingLayout(window);
            return;
        }

        var left = work.Left + Math.Max(0, ((work.Right - work.Left) - width) / 2);
        var top = work.Top + Math.Max(0, ((work.Bottom - work.Top) - height) / 2);

        _ = User32.SetWindowPos(handle, IntPtr.Zero, left, top, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
    }

    private static void ApplyHeightCap(Window window)
    {
        var area = GetPointerMonitorWorkArea(window);

        window.MaxHeight = Math.Max(MinimumHeight, area.Height - (MinimumMargin * 2));
    }

    /// <summary>
    /// Fallback used before layout has produced a window rectangle: places the window using its
    /// own device-independent size.
    /// </summary>
    private static void PlaceUsingLayout(Window window)
    {
        var area = GetPointerMonitorWorkArea(window);
        var width = ResolveSize(window.ActualWidth, window.Width, 480d);
        var height = ResolveSize(window.ActualHeight, window.Height, 360d);

        window.Left = area.Left + Math.Max(0d, (area.Width - width) / 2d);
        window.Top = area.Top + Math.Max(0d, (area.Height - height) / 2d);
    }

    private static bool TryGetPointerWorkArea(out User32.RECT work)
    {
        work = default;

        if (!User32.GetCursorPos(out var pointer))
        {
            return false;
        }

        var monitor = User32.MonitorFromPoint(pointer, User32.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        var info = new User32.MONITORINFO { cbSize = Marshal.SizeOf<User32.MONITORINFO>() };
        if (!User32.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        work = info.rcWork;
        return true;
    }

    private static double ResolveSize(double actual, double declared, double fallback)
    {
        if (!double.IsNaN(actual) && actual > 0)
        {
            return actual;
        }

        return !double.IsNaN(declared) && declared > 0 ? declared : fallback;
    }

    private static Point GetDeviceToDiuScale(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var transform = HwndSource.FromHwnd(handle)?.CompositionTarget?.TransformFromDevice;

        return new Point(transform?.M11 ?? 1d, transform?.M22 ?? 1d);
    }
}
