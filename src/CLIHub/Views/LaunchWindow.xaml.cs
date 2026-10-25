namespace CLIHub.Views;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Interop;
using CLIHub.ViewModels;
using CLIHub.Startup;

/// <summary>
///   Interaction logic for LaunchWindow.xaml. The window is a chromeless popup shell: it stays on
///   top, hides when it loses focus unless pinned, and is centred on the pointer's monitor.
/// </summary>
public partial class LaunchWindow : Window, IStartupWindow
{
    /// <summary>
    ///   Window during which a deactivation right after showing is ignored: when Windows denies
    ///   foreground activation (showing from the tray or the hotkey), the freshly shown window would
    ///   otherwise hide itself immediately.
    /// </summary>
    private const long DeactivationGraceMilliseconds = 400;

    private readonly LaunchWindowViewModel viewModel;
    private readonly PromptState promptState;
    private readonly IPreferencesStore preferencesStore;
    private const double SplitterHitTolerance = 6d;

    private long shownAt;
    private bool draggingSplitter;
    private double dragStartX;
    private double dragStartProjectsWidth;
    private bool closed;

    /// <summary>
    ///   Creates the window and binds it to the shared view model.
    /// </summary>
    /// <param name="viewModel"> The launch window view model. </param>
    /// <param name="promptState"> Modal prompt tracker used to keep the window visible. </param>
    /// <param name="preferencesStore"> Preferences source for the popup screen position. </param>
    public LaunchWindow(
        LaunchWindowViewModel viewModel,
        PromptState promptState,
        IPreferencesStore preferencesStore)
    {
        this.InitializeComponent();

        this.viewModel = viewModel;
        this.promptState = promptState;
        this.preferencesStore = preferencesStore;
        this.DataContext = viewModel;
    }

    /// <summary>
    ///   Shows the window at the configured popup screen position. A request that arrives after the
    ///   window has closed — during application shutdown, while the tray icon and activation
    ///   subscriptions are still live — is ignored: it does no work at all.
    /// </summary>
    public void ShowOnPointerMonitor()
    {
        if (this.closed)
        {
            return;
        }

        var position = this.preferencesStore.Load().PopupScreenPosition;

        WindowPositioner.Place(this, position);
        this.viewModel.RefreshProjects();

        // Refresh the agent pane too, so installation state, availability, and logos are current
        // while the window is visible; version re-population is served by the version cache
        // within its TTL. The default-agent lookup must run against the fresh rows.
        this.viewModel.RefreshAgents();
        this.viewModel.ApplyDefaultAgentSelection();
        this.Show();

        if (this.WindowState == WindowState.Minimized)
        {
            this.WindowState = WindowState.Normal;
        }

        this.shownAt = Environment.TickCount64;
        this.Activate();

        // SizeToContent settles during the first layout pass, so place again once the real
        // size is known. The deferred placement goes through PlaceWhenOpen so it is ignored
        // when the window has closed by the time the callback runs.
        this.Dispatcher.BeginInvoke(
            new Action<PopupScreenPosition>(this.PlaceWhenOpen),
            DispatcherPriority.Loaded,
            position);
    }

    /// <summary>
    ///   Places the window at <paramref name="position" />. Used for the deferred placement queued
    ///   after <c>Show</c>, which runs when the layout pass settles and can arrive after the
    ///   window has closed; the request is ignored in that case.
    /// </summary>
    /// <param name="position"> The configured popup screen position. </param>
    internal void PlaceWhenOpen(PopupScreenPosition position)
    {
        if (this.closed)
        {
            return;
        }

        WindowPositioner.Place(this, position);
    }

    private void OnSourceInitialized(object? sender, EventArgs e) =>
        DwmApi.TryRoundCorners(new WindowInteropHelper(this).EnsureHandle());

    private void OnWindowActivated(object? sender, EventArgs e)
    {
        this.viewModel.RefreshProjects();

        // Version re-population on this refresh is cache-served within the probe TTL.
        this.viewModel.RefreshAgents();
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        if (this.viewModel.IsPinned || this.promptState.IsPromptOpen)
        {
            return;
        }

        if (Environment.TickCount64 - this.shownAt < DeactivationGraceMilliseconds)
        {
            return;
        }

        this.Hide();
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

        if (!this.CloseActionsMenus())
        {
            this.Hide();
        }

        e.Handled = true;
    }

    /// <summary>
    ///   Closes the actions menu on Escape while the keyboard focus is inside the popup. A popup is
    ///   a separate window, so its key events do not reach the window's <c> OnKeyDown </c>.
    /// </summary>
    private void OnActionsMenuPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.CloseActionsMenus();
            e.Handled = true;
        }
    }

    /// <summary>
    ///   Closes both actions menus.
    /// </summary>
    /// <returns> True when either menu was open. </returns>
    private bool CloseActionsMenus()
    {
        var wasOpen = this.ProjectsActionsPopup.IsOpen || this.AgentsActionsPopup.IsOpen;

        this.ProjectsActionsPopup.IsOpen = false;
        this.AgentsActionsPopup.IsOpen = false;

        return wasOpen;
    }

    /// <summary>
    ///   Starts a pane resize when the press lands next to the divider. The press is handled on the
    ///   window (with a hit tolerance) because a 1px divider is hard to hit exactly.
    /// </summary>
    private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || this.IsInsidePane(source))
        {
            return;
        }

        var dividerCenter = this.PanesGrid.ColumnDefinitions[0].ActualWidth + (this.PaneSplitter.ActualWidth / 2);
        var position = e.GetPosition(this);

        if (Math.Abs(position.X - dividerCenter) > SplitterHitTolerance)
        {
            return;
        }

        this.draggingSplitter = true;
        this.dragStartX = position.X;
        this.dragStartProjectsWidth = this.PanesGrid.ColumnDefinitions[0].ActualWidth;
        this.CaptureMouse();
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
            if (ReferenceEquals(current, this.PaneSplitter))
            {
                return false;
            }

            if (ReferenceEquals(current, this.PanesGrid))
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
        var projects = this.PanesGrid.ColumnDefinitions[0];
        var agents = this.PanesGrid.ColumnDefinitions[2];

        agents.Width = GridLength.Auto;

        var maxWidth = Math.Max(projects.MinWidth, SystemParameters.WorkArea.Width - agents.MinWidth - 40);

        projects.Width = new GridLength(Math.Clamp(width, projects.MinWidth, maxWidth));
    }

    private void OnSplitterMouseMove(object sender, MouseEventArgs e)
    {
        if (!this.draggingSplitter)
        {
            return;
        }

        this.ApplyProjectsPaneWidth(this.dragStartProjectsWidth + (e.GetPosition(this).X - this.dragStartX));
    }

    private void OnSplitterMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!this.draggingSplitter)
        {
            return;
        }

        this.draggingSplitter = false;
        this.ReleaseMouseCapture();
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
    private void OnMenuActionInvoked(object sender, RoutedEventArgs e) => this.CloseActionsMenus();

    /// <summary>
    ///   Marks the window as closed before the base close bookkeeping runs. During application
    ///   shutdown WPF closes the window even though <see cref="OnClosing" /> cancels, so this is
    ///   the single point that records that the window cannot be reopened after application
    ///   shutdown has closed it; show requests that still arrive afterwards are ignored.
    /// </summary>
    /// <param name="e"> Closed event arguments. </param>
    protected override void OnClosed(EventArgs e)
    {
        this.closed = true;
        base.OnClosed(e);
    }

    /// <summary>
    ///   Hides the window instead of closing it; the application lives in the tray.
    /// </summary>
    /// <param name="e"> Closing event arguments, always cancelled. </param>
    protected override void OnClosing(CancelEventArgs e)
    {
        // Hide instead of exiting: the window is put away with Escape, the tray menu, or Exit.
        e.Cancel = true;
        this.Hide();
        base.OnClosing(e);
    }
}
