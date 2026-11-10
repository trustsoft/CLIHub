namespace CLIHub.Views;

using System.Runtime.InteropServices;

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

using CLIHub.Interop;
using CLIHub.ViewModels;

/// <summary>
///   Browse Plugins dialog: displays the remote plugin feed with install/update actions.
/// </summary>
public partial class BrowsePluginsDialog : Window
{
    /// <summary>
    ///   Creates the dialog.
    /// </summary>
    /// <param name="viewModel"> The dialog view model. </param>
    public BrowsePluginsDialog(BrowsePluginsDialogViewModel viewModel)
    {
        this.InitializeComponent();

        this.DataContext = viewModel;

        this.Loaded += async (sender, args) => await viewModel.LoadAsync();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        DwmApi.TryRoundCorners(handle);
        HwndSource.FromHwnd(handle)?.AddHook(this.OnWindowMessage);
    }

    /// <summary>
    ///   Enforces <see cref="System.Windows.FrameworkElement.MinWidth" />/
    ///   <see cref="System.Windows.FrameworkElement.MinHeight" /> as the minimum tracking size, so
    ///   the dialog cannot be dragged below them. WPF's own handler is bypassed here because the
    ///   message is marked handled, so the size conversion from layout units to physical pixels
    ///   (using the window's DPI) is done explicitly.
    /// </summary>
    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != User32.WM_GETMINMAXINFO)
        {
            return IntPtr.Zero;
        }

        var dpi = User32.GetDpiForWindow(hwnd);
        var scale = dpi == 0 ? 1.0 : dpi / 96.0;

        var minMax = Marshal.PtrToStructure<User32.MINMAXINFO>(lParam);
        minMax.ptMinTrackSize.X = (int)Math.Ceiling(this.MinWidth * scale);
        minMax.ptMinTrackSize.Y = (int)Math.Ceiling(this.MinHeight * scale);
        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    /// <summary>
    ///   Moves the dialog while the drawn header is dragged. The window buttons handle the press
    ///   themselves, so a click on one never starts a drag.
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

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

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
