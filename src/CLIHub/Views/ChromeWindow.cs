namespace CLIHub.Views;

using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;

using CLIHub.Interop;

/// <summary>
///   Shared base for the chromeless secondary windows (Settings, What's New, Browse Plugins). The
///   constructor applies the borderless profile in code — no OS window chrome, the shared
///   <see cref="WindowChrome"/> metrics, the palette's chrome background, and the shared text font —
///   and the base owns DWM rounded corners, the window message hook enforcing the minimum tracking
///   size, Escape-to-close, and the drawn header's drag move. The launch
///   window deliberately does not use this base: it is a topmost content-sized popup with no resize
///   borders, and Escape hides it instead of closing it.
/// </summary>
/// <remarks>
///   The class is public because a XAML-rooted window class cannot derive from an internal base:
///   the markup compiler generates the window's other partial as public (<c>x:ClassModifier</c> is
///   rejected by the .NET markup compiler with MC6013). Derived windows are expected to stay in the
///   application assembly.
/// </remarks>
public abstract class ChromeWindow : Window
{
    /// <summary>
    ///   Initializes the shared chromeless profile. <c>GlassFrameThickness</c> 0 removes the native
    ///   border the DWM draws around a resizable borderless window, so the visible border matches
    ///   the 1px line every derived window draws itself.
    /// </summary>
    protected ChromeWindow()
    {
        this.WindowStyle = WindowStyle.None;
        WindowChrome.SetWindowChrome(
            this,
            new WindowChrome
            {
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                ResizeBorderThickness = new Thickness(6),
                UseAeroCaptionButtons = false
            });
        this.SetResourceReference(BackgroundProperty, "ChromeBgBrush");
        this.SetResourceReference(FontFamilyProperty, "Sizing.FontFamily.Text");
    }

    /// <summary>
    ///   Applies DWM rounded corners and registers the window message hook when the window handle
    ///   is created, so both live for the window's lifetime.
    /// </summary>
    /// <param name="e"> The event data. </param>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        WindowChromeHelper.ApplyRoundedCorners(this);
        HwndSource.FromHwnd(new WindowInteropHelper(this).EnsureHandle())?.AddHook(this.OnWindowMessage);
    }

    /// <summary>
    ///   Closes the window on Escape unless an inner element already handled the key.
    /// </summary>
    /// <param name="e"> The key event data. </param>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!e.Handled && e.Key == Key.Escape)
        {
            this.Close();
            e.Handled = true;
        }
    }

    /// <summary>
    ///   Moves the window while the drawn header is dragged. A press on a header button never
    ///   reaches this helper because buttons handle the press themselves.
    /// </summary>
    /// <param name="e"> The header's mouse-down event data. </param>
    internal void BeginHeaderDrag(MouseButtonEventArgs e)
    {
        try
        {
            this.DragMove();
        }
        catch (InvalidOperationException)
        {
            // The button was released before the move started.
        }
    }

    /// <summary>
    ///   Enforces the window's declared minimum size as the minimum tracking size. Because handling
    ///   <see cref="User32.WM_GETMINMAXINFO" /> bypasses WPF's own handler, the minimum tracking
    ///   size is set here so the window cannot be resized below
    ///   <see cref="System.Windows.FrameworkElement.MinWidth" />/
    ///   <see cref="System.Windows.FrameworkElement.MinHeight" />.
    /// </summary>
    /// <param name="hwnd"> The window handle the message was received for. </param>
    /// <param name="msg"> The message id. </param>
    /// <param name="wParam"> The message's wParam. </param>
    /// <param name="lParam"> The message's lParam. </param>
    /// <param name="handled"> Whether the message was handled. </param>
    /// <returns> Always <see cref="IntPtr.Zero" />. </returns>
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
}
