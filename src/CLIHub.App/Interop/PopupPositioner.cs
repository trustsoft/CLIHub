using System.Windows;
using System.Windows.Interop;

namespace CLIHub.App.Interop;

public static class PopupPositioner
{
    public static void PositionCenteredOnCursorScreen(Window window)
    {
        var handle = new WindowInteropHelper(window).EnsureHandle();
        var source = HwndSource.FromHwnd(handle);
        var transform = source?.CompositionTarget?.TransformFromDevice;
        var scaleX = transform?.M11 ?? 1d;
        var scaleY = transform?.M22 ?? 1d;

        NativeMethods.GetCursorPos(out var cursor);
        var screen = System.Windows.Forms.Screen.FromPoint(new System.Drawing.Point(cursor.X, cursor.Y));
        var workingArea = screen.WorkingArea;

        var areaLeft = workingArea.Left * scaleX;
        var areaTop = workingArea.Top * scaleY;
        var areaWidth = workingArea.Width * scaleX;
        var areaHeight = workingArea.Height * scaleY;

        var left = areaLeft + ((areaWidth - window.Width) / 2);
        var top = areaTop + ((areaHeight - window.Height) / 2);

        left = Math.Max(areaLeft, Math.Min(left, areaLeft + areaWidth - window.Width));
        top = Math.Max(areaTop, Math.Min(top, areaTop + areaHeight - window.Height));

        window.Left = left;
        window.Top = top;
    }
}
