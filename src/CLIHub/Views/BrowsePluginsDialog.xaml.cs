namespace CLIHub.Views;

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
        WindowChromeHelper.ApplyRoundedCorners(this);
        var handle = new WindowInteropHelper(this).EnsureHandle();
        HwndSource.FromHwnd(handle)?.AddHook(this.OnWindowMessage);
    }

    /// <summary>
    ///   Enforces <see cref="System.Windows.FrameworkElement.MinWidth" />/
    ///   <see cref="System.Windows.FrameworkElement.MinHeight" /> as the minimum tracking size,
    ///   so the dialog cannot be dragged below them, by delegating to
    ///   <see cref="WindowChromeHelper.ApplyMinTrackSize" />.
    /// </summary>
    private IntPtr OnWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != User32.WM_GETMINMAXINFO)
        {
            return IntPtr.Zero;
        }

        WindowChromeHelper.ApplyMinTrackSize(this, hwnd, lParam);
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
