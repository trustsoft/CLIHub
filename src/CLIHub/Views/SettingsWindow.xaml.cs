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
        this.Closed += (_, _) => this.viewModel.Dispose();
    }

    /// <summary>
    ///   Reloads the stored preferences and shows (or re-activates) the window. The window grows to
    ///   the launch window's height when that window is taller, but it never shrinks below its own
    ///   size.
    /// </summary>
    public void ShowSettings()
    {
        this.viewModel.Load();

        WindowChromeHelper.EnsureOwnedByLaunchWindow(this);

        if (Application.Current?.MainWindow is { } launch && launch.ActualHeight > this.Height)
        {
            this.Height = launch.ActualHeight;
        }

        WindowChromeHelper.ShowAndActivate(this);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        WindowChromeHelper.ApplyRoundedCorners(this);
        var handle = new WindowInteropHelper(this).EnsureHandle();
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

        var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);
        minMax.ptMaxPosition.X = info.rcWork.Left - info.rcMonitor.Left;
        minMax.ptMaxPosition.Y = info.rcWork.Top - info.rcMonitor.Top;
        minMax.ptMaxSize.X = info.rcWork.Right - info.rcWork.Left;
        minMax.ptMaxSize.Y = info.rcWork.Bottom - info.rcWork.Top;
        WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);
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
