namespace CLIHub.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Win32 declarations from dwmapi.dll.
/// </summary>
internal static partial class DwmApi
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwcpRound = 2;

    /// <summary>
    /// Asks the desktop window manager to round the window's corners. The call is ignored on
    /// operating systems that do not support the attribute, which leaves square corners.
    /// </summary>
    internal static void TryRoundCorners(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        var preference = DwmwcpRound;
        _ = DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}
