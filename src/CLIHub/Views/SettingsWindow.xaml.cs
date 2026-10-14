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
    private readonly SettingsViewModel viewModel;

    /// <summary>
    ///   Creates the window and binds it to the settings view model.
    /// </summary>
    /// <param name="viewModel"> The settings view model. </param>
    public SettingsWindow(SettingsViewModel viewModel)
    {
        this.InitializeComponent();

        this.viewModel = viewModel;
        this.DataContext = viewModel;
        viewModel.RequestClose += (_, _) => this.Close();
        Closed += (_, _) => this.viewModel.Dispose();
    }

    /// <summary>
    ///   Reloads the stored preferences and shows (or re-activates) the window. The height matches
    ///   the launch window's current height, so the two windows read as the same surface.
    /// </summary>
    public void ShowSettings()
    {
        this.viewModel.Load();

        // The launch window stays on top of other windows, so the Settings window has to be owned
        // by it to be visible above it.
        if (this.Owner is null && Application.Current?.MainWindow is { } owner && !ReferenceEquals(owner, this))
        {
            this.Owner = owner;
        }

        if (Application.Current?.MainWindow is { } launch)
        {
            this.Height = Math.Max(this.MinHeight, launch.ActualHeight);
        }

        this.Show();

        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = WindowState.Normal;
        }

        this.Activate();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        DwmApi.TryRoundCorners(handle);
        HwndSource.FromHwnd(handle)?.AddHook(this.OnWindowMessage);
    }

    /// <summary>
    ///   Constrains a maximized window to the monitor work area, so the taskbar never hides the footer.
    ///   Because handling this message bypasses WPF's own handler, the minimum tracking size is set
    ///   here too, so the window cannot be resized below
    ///   <see cref="System.Windows.FrameworkElement.MinWidth" />/
    ///   <see cref="System.Windows.FrameworkElement.MinHeight" />.
    /// </summary>
    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
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

        var dpi = User32.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;

        var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);
        minMax.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
        minMax.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
        minMax.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
        minMax.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
        minMax.ptMinTrackSize.X = (int)Math.Ceiling(this.MinWidth * scale);
        minMax.ptMinTrackSize.Y = (int)Math.Ceiling(this.MinHeight * scale);
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
            this.DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move started.
        }
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => this.Close();

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        this.Close();
        e.Handled = true;
    }
}
