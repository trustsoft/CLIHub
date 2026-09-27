using System.Runtime.InteropServices;

namespace CLIHub.Interop;

/// <summary>
/// Win32 declarations from user32.dll.
/// </summary>
internal static partial class User32
{
    /// <summary>
    /// Posted when the user presses a hot key registered by <see cref="RegisterHotKey"/>.
    /// </summary>
    internal const int WM_HOTKEY = 0x0312;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnregisterHotKey(IntPtr hWnd, int id);
}
