namespace CLIHub.Views;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using CLIHub.Interop;
using CLIHub.ViewModels;

/// <summary>
///   Interaction logic for LaunchWindow.xaml. The window is a chromeless popup shell: it stays on
///   top, hides when it loses focus unless pinned, and is centred on the pointer's monitor.
/// </summary>
public partial class LaunchWindow : Window
{
    /// <summary>
    ///   Window during which a deactivation right after showing is ignored: when Windows denies
    ///   foreground activation (showing from the tray or the hotkey), the freshly shown window would
    ///   otherwise hide itself immediately.
    /// </summary>
    private const long DeactivationGraceMilliseconds = 400;

    private readonly LaunchWindowViewModel _viewModel;
    private readonly PromptState _promptState;
    private const double SplitterHitTolerance = 6d;

    private long _shownAt;
    private bool _draggingSplitter;
    private double _dragStartX;
    private double _dragStartProjectsWidth;

    /// <summary>
    ///   Creates the window and binds it to the shared view model.
    /// </summary>
    /// <param name="viewModel"> The launch window view model. </param>
    /// <param name="promptState"> Modal prompt tracker used to keep the window visible. </param>
    public LaunchWindow(LaunchWindowViewModel viewModel, PromptState promptState)
    {
        InitializeComponent();

        _viewModel = viewModel;
        _promptState = promptState;
        DataContext = viewModel;
    }

    /// <summary>
    ///   Shows the window centred in the work area of the monitor that contains the pointer.
    /// </summary>
    public void ShowOnPointerMonitor()
    {
        WindowPositioner.PlaceOnPointerMonitor(this);
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        _shownAt = Environment.TickCount64;
        Activate();

        // SizeToContent settles during the first layout pass, so centre again once the real
        // size is known.
        Dispatcher.BeginInvoke(
            new Action<Window>(WindowPositioner.PlaceOnPointerMonitor),
            DispatcherPriority.Loaded,
            this);
    }

    private void OnSourceInitialized(object? sender, EventArgs e) =>
        DwmApi.TryRoundCorners(new WindowInteropHelper(this).EnsureHandle());

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (_viewModel.IsPinned || _promptState.IsPromptOpen)
        {
            return;
        }

        if (Environment.TickCount64 - _shownAt < DeactivationGraceMilliseconds)
        {
            return;
        }

        Hide();
    }

    /// <summary>
    ///   Closes an open actions menu on Escape; otherwise hides the window. A second Escape, after
    ///   the menu is closed, hides the window.
    /// </summary>
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (!CloseActionsMenus())
        {
            Hide();
        }

        e.Handled = true;
    }

    /// <summary>
    ///   Closes the actions menu on Escape while the keyboard focus is inside the popup. A popup is
    ///   a separate window, so its key events do not reach the window's <c>OnKeyDown</c>.
    /// </summary>
    private void OnActionsMenuPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CloseActionsMenus();
            e.Handled = true;
        }
    }

    /// <summary>
    ///   Closes both actions menus.
    /// </summary>
    /// <returns> True when either menu was open. </returns>
    private bool CloseActionsMenus()
    {
        var wasOpen = ProjectsActionsPopup.IsOpen || AgentsActionsPopup.IsOpen;

        ProjectsActionsPopup.IsOpen = false;
        AgentsActionsPopup.IsOpen = false;

        return wasOpen;
    }

    /// <summary>
    ///   Starts a pane resize when the press lands next to the divider. The press is handled on the
    ///   window (with a hit tolerance) because a 1px divider is hard to hit exactly.
    /// </summary>
    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || IsInsidePane(source))
        {
            return;
        }

        var dividerCenter = PanesGrid.ColumnDefinitions[0].ActualWidth + (PaneSplitter.ActualWidth / 2);
        var position = e.GetPosition(this);

        if (Math.Abs(position.X - dividerCenter) > SplitterHitTolerance)
        {
            return;
        }

        _draggingSplitter = true;
        _dragStartX = position.X;
        _dragStartProjectsWidth = PanesGrid.ColumnDefinitions[0].ActualWidth;
        CaptureMouse();
        e.Handled = true;
    }

    /// <summary>
    ///   True when the pressed element belongs to one of the two panes, so only presses on the
    ///   divider area itself start a resize.
    /// </summary>
    private bool IsInsidePane(DependencyObject source)
    {
        for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (ReferenceEquals(current, PaneSplitter))
            {
                return false;
            }

            if (ReferenceEquals(current, PanesGrid))
            {
                return true;
            }
        }

        return true;
    }

    /// <summary>
    ///   Sizes the Projects pane from a drag: the Agents pane stays content sized, so the window
    ///   (which sizes itself to its content) follows the dragged width. The width is clamped to the
    ///   pane minimum, so its list is never clipped.
    /// </summary>
    private void ApplyProjectsPaneWidth(double width)
    {
        var projects = PanesGrid.ColumnDefinitions[0];
        var agents = PanesGrid.ColumnDefinitions[2];

        agents.Width = GridLength.Auto;

        var maxWidth = Math.Max(projects.MinWidth, SystemParameters.WorkArea.Width - agents.MinWidth - 40);

        projects.Width = new GridLength(Math.Clamp(width, projects.MinWidth, maxWidth));
    }

    private void OnSplitterMouseMove(object sender, MouseEventArgs e)
    {
        if (!_draggingSplitter)
        {
            return;
        }

        ApplyProjectsPaneWidth(_dragStartProjectsWidth + (e.GetPosition(this).X - _dragStartX));
    }

    private void OnSplitterMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_draggingSplitter)
        {
            return;
        }

        _draggingSplitter = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    /// <summary>
    ///   Right-aligns an actions popover with the control that opened it. The popover sizes to its
    ///   content, so its width is measured here rather than fixed in XAML.
    /// </summary>
    private void OnActionsPopupOpened(object? sender, EventArgs e)
    {
        if (sender is not Popup popup || popup.Child is not FrameworkElement child)
        {
            return;
        }

        child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var targetWidth = popup.PlacementTarget is FrameworkElement target ? target.ActualWidth : 0;
        popup.HorizontalOffset = targetWidth - child.DesiredSize.Width;
    }

    /// <summary>
    ///   Closes the actions menu the clicked entry belongs to. A popup does not close when an entry
    ///   inside it is clicked, so the click handler closes both.
    /// </summary>
    private void OnMenuActionInvoked(object sender, RoutedEventArgs e) => CloseActionsMenus();

    /// <summary>
    ///   Hides the window instead of closing it; the application lives in the tray.
    /// </summary>
    /// <param name="e"> Closing event arguments, always cancelled. </param>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Hide instead of exiting: the window is put away with Escape, the tray menu, or Exit.
        e.Cancel = true;
        Hide();
        base.OnClosing(e);
    }
}
