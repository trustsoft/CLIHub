namespace CLIHub.Interop;

using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using CLIHub.Core.Models;

/// <summary>
///   Places the launch window according to the configured popup screen position, without
///   depending on Windows Forms.
/// </summary>
internal static class WindowPositioner
{
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const double MinimumMargin = 8d;
    private const double MinimumHeight = 240d;
    private const int TopMargin = 24;
    private const int CursorOffset = 12;

    /// <summary>
    ///   Places the window according to <paramref name="position"/>. The move itself is done in
    ///   native screen coordinates: the window and the monitor work area are both measured by the
    ///   operating system, so the result stays correct when the monitors use different scale factors.
    /// </summary>
    internal static void Place(Window window, PopupScreenPosition position)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();

        ApplyHeightCap(window, position);

        if (!TryGetWorkArea(position, out var work) || !User32.GetWindowRect(handle, out var current))
        {
            PlaceUsingLayout(window, position);
            return;
        }

        var width = current.Right - current.Left;
        var height = current.Bottom - current.Top;

        if (width <= 0 || height <= 0)
        {
            PlaceUsingLayout(window, position);
            return;
        }

        var (left, top) = ComputePosition(position, work, width, height);

        _ = User32.SetWindowPos(handle, IntPtr.Zero, left, top, 0, 0, SwpNoSize | SwpNoZOrder | SwpNoActivate);
    }

    private static (int Left, int Top) ComputePosition(
        PopupScreenPosition position,
        in User32.RECT work,
        int width,
        int height)
    {
        var workWidth = work.Right - work.Left;
        var workHeight = work.Bottom - work.Top;

        var left = position switch
        {
            PopupScreenPosition.CursorRelative => work.Left + CursorOffset,
            _ => work.Left + Math.Max(0, (workWidth - width) / 2)
        };

        var top = position switch
        {
            PopupScreenPosition.TopCenter => work.Top + TopMargin,
            PopupScreenPosition.CursorRelative => work.Top + CursorOffset,
            _ => work.Top + Math.Max(0, (workHeight - height) / 2)
        };

        if (position == PopupScreenPosition.CursorRelative && User32.GetCursorPos(out var pointer))
        {
            left = pointer.X + CursorOffset;
            top = pointer.Y + CursorOffset;
        }

        return Clamp(work, left, top, width, height);
    }

    private static (int Left, int Top) Clamp(in User32.RECT work, int left, int top, int width, int height)
    {
        var maxLeft = Math.Max(work.Left, work.Right - width);
        var maxTop = Math.Max(work.Top, work.Bottom - height);

        return (Math.Clamp(left, work.Left, maxLeft), Math.Clamp(top, work.Top, maxTop));
    }

    private static void ApplyHeightCap(Window window, PopupScreenPosition position)
    {
        var area = GetWorkArea(window, position);

        window.MaxHeight = Math.Max(MinimumHeight, area.Height - (MinimumMargin * 2));
    }

    /// <summary>
    ///   Fallback used before layout has produced a window rectangle: places the window using its
    ///   own device-independent size.
    /// </summary>
    private static void PlaceUsingLayout(Window window, PopupScreenPosition position)
    {
        var area = GetWorkArea(window, position);
        var width = ResolveSize(window.ActualWidth, window.Width, 480d);
        var height = ResolveSize(window.ActualHeight, window.Height, 360d);

        window.Left = area.Left + Math.Max(0d, (area.Width - width) / 2d);
        window.Top = area.Top + Math.Max(0d, (area.Height - height) / 2d);
    }

    private static Rect GetWorkArea(Window window, PopupScreenPosition position)
    {
        var scale = GetDeviceToDiuScale(window);

        if (TryGetWorkArea(position, out var work))
        {
            return new Rect(
                work.Left * scale.X,
                work.Top * scale.Y,
                (work.Right - work.Left) * scale.X,
                (work.Bottom - work.Top) * scale.Y);
        }

        return SystemParameters.WorkArea;
    }

    private static bool TryGetWorkArea(PopupScreenPosition position, out User32.RECT work)
    {
        work = default;

        IntPtr monitor;
        if (position == PopupScreenPosition.PrimaryScreen)
        {
            monitor = User32.MonitorFromPoint(
                new User32.POINT { X = 0, Y = 0 },
                User32.MONITOR_DEFAULTTOPRIMARY);
        }
        else
        {
            if (!User32.GetCursorPos(out var pointer))
            {
                return false;
            }

            monitor = User32.MonitorFromPoint(pointer, User32.MONITOR_DEFAULTTONEAREST);
        }

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
