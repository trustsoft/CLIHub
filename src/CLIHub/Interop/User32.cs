namespace CLIHub.Interop;

using System.Runtime.InteropServices;

/// <summary>
/// Win32 declarations from user32.dll.
/// </summary>
internal static partial class User32
{
    /// <summary>
    /// Posted when the user presses a hot key registered by <see cref="RegisterHotKey"/>.
    /// </summary>
    internal const int WM_HOTKEY = 0x0312;

    /// <summary>
    /// Returns the monitor nearest to a point when the point is not inside any monitor.
    /// </summary>
    internal const uint MONITOR_DEFAULTTONEAREST = 2;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>
    /// Returns the pointer position in physical pixels.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetCursorPos(out POINT point);

    /// <summary>
    /// Returns the handle of the monitor that contains the point, or the nearest one.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial IntPtr MonitorFromPoint(POINT point, uint flags);

    /// <summary>
    /// Fills <paramref name="info"/> with the monitor's bounds and work area (physical pixels).
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    /// <summary>
    /// Moves a window in native screen coordinates.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    /// <summary>
    /// Returns the window rectangle in native screen coordinates.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [StructLayout(LayoutKind.Sequential)]
    internal struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }
}
