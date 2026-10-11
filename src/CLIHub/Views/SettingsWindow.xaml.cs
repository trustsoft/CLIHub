namespace CLIHub.Views;

using System.Runtime.InteropServices;

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using CLIHub.Interop;
using CLIHub.ViewModels;

/// <summary>
///   The Settings window: dark drawn chrome (no OS title bar) around sidebar-navigated pages.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    /// <summary>
    ///   Creates the window and binds it to the settings view model.
    /// </summary>
    /// <param name="viewModel"> The settings view model. </param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.RequestClose += (_, _) => Close();
        Closed += (_, _) => _viewModel.Dispose();
    }

    /// <summary>
    ///   Reloads the stored preferences and shows (or re-activates) the window. The height matches
    ///   the launch window's current height, so the two windows read as the same surface.
    /// </summary>
    public void ShowSettings()
    {
        _viewModel.Load();

        // The launch window stays on top of other windows, so the Settings window has to be owned
        // by it to be visible above it.
        if (Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this))
        {
            Owner = owner;
        }

        if (Application.Current?.MainWindow is { } launch)
        {
            Height = Math.Max(MinHeight, launch.ActualHeight);
        }

        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        DwmApi.TryRoundCorners(handle);
        HwndSource.FromHwnd(handle)?.AddHook(OnWindowMessage);
    }

    /// <summary>
    ///   Constrains a maximized window to the monitor work area, so the taskbar never hides the footer.
    /// </summary>
    private static IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != User32.WM_GETMINMAXINFO)
        {
            return IntPtr.Zero;
        }

        var monitor = User32.MonitorFromWindow(hwnd, User32.MONITOR_DEFAULTTONEAREST);
        if (monitor == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        var info = new User32.MONITORINFO { cbSize = Marshal.SizeOf<User32.MONITORINFO>() };
        if (!User32.GetMonitorInfo(monitor, ref info))
        {
            return IntPtr.Zero;
        }

        var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);
        minMax.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
        minMax.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
        minMax.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
        minMax.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    /// <summary>
    ///   Moves the window while the drawn header is dragged. The close button handles the press
    ///   itself, so a click on it never starts a drag.
    /// </summary>
    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move started.
        }
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        Close();
        e.Handled = true;
    }
}
