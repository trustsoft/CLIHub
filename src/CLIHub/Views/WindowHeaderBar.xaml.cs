namespace CLIHub.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

/// <summary>
///   The drawn header strip shared by the chromeless windows built on
///   <see cref="ChromeWindow" />: a title with optional window icon, an action slot for
///   window-specific buttons, an optional maximize/restore toggle, the close button, and an
///   optional separator hairline. The maximize and close buttons only raise
///   <see cref="MaximizeRequested" /> and <see cref="CloseRequested" />; the hosting window decides
///   the action.
/// </summary>
public partial class WindowHeaderBar : UserControl
{
    /// <summary>
    ///   Initializes the control's XAML content.
    /// </summary>
    public WindowHeaderBar()
    {
        this.InitializeComponent();
    }

    /// <summary> Identifies the <see cref="Title" /> dependency property. </summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title),
        typeof(string),
        typeof(WindowHeaderBar),
        new PropertyMetadata(string.Empty));

    /// <summary> Identifies the <see cref="IconSource" /> dependency property. </summary>
    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
        nameof(IconSource),
        typeof(ImageSource),
        typeof(WindowHeaderBar),
        new PropertyMetadata(null));

    /// <summary> Identifies the <see cref="Actions" /> dependency property. </summary>
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions),
        typeof(object),
        typeof(WindowHeaderBar),
        new PropertyMetadata(null));

    /// <summary> Identifies the <see cref="ShowMaximize" /> dependency property. </summary>
    public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register(
        nameof(ShowMaximize),
        typeof(bool),
        typeof(WindowHeaderBar),
        new PropertyMetadata(false));

    /// <summary> Identifies the <see cref="ShowSeparator" /> dependency property. </summary>
    public static readonly DependencyProperty ShowSeparatorProperty = DependencyProperty.Register(
        nameof(ShowSeparator),
        typeof(bool),
        typeof(WindowHeaderBar),
        new PropertyMetadata(true));

    /// <summary> Identifies the <see cref="CloseRequested" /> routed event. </summary>
    public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(CloseRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(WindowHeaderBar));

    /// <summary> Identifies the <see cref="MaximizeRequested" /> routed event. </summary>
    public static readonly RoutedEvent MaximizeRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(MaximizeRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(WindowHeaderBar));

    /// <summary>
    ///   Gets or sets the header title text. Every hosting window sets it.
    /// </summary>
    public string Title
    {
        get => (string)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    /// <summary>
    ///   Gets or sets the header icon. Settings and Browse Plugins set it for the icon + compact
    ///   title variant; What's New leaves it null for the text-only title variant.
    /// </summary>
    public ImageSource? IconSource
    {
        get => (ImageSource?)this.GetValue(IconSourceProperty);
        set => this.SetValue(IconSourceProperty, value);
    }

    /// <summary>
    ///   Gets or sets the action slot content rendered ahead of the maximize and close buttons.
    ///   What's New hosts its Check/Install buttons here.
    /// </summary>
    public object? Actions
    {
        get => this.GetValue(ActionsProperty);
        set => this.SetValue(ActionsProperty, value);
    }

    /// <summary>
    ///   Gets or sets a value indicating whether the maximize/restore toggle is shown. Only
    ///   Settings sets it.
    /// </summary>
    public bool ShowMaximize
    {
        get => (bool)this.GetValue(ShowMaximizeProperty);
        set => this.SetValue(ShowMaximizeProperty, value);
    }

    /// <summary>
    ///   Gets or sets a value indicating whether the separator hairline renders under the header.
    ///   What's New sets it to false — its hairline is a window-bottom line instead.
    /// </summary>
    public bool ShowSeparator
    {
        get => (bool)this.GetValue(ShowSeparatorProperty);
        set => this.SetValue(ShowSeparatorProperty, value);
    }

    /// <summary>
    ///   Occurs when the close button is activated. The hosting window closes itself.
    /// </summary>
    public event RoutedEventHandler CloseRequested
    {
        add { this.AddHandler(CloseRequestedEvent, value); }
        remove { this.RemoveHandler(CloseRequestedEvent, value); }
    }

    /// <summary>
    ///   Occurs when the maximize/restore toggle is activated. The hosting window flips its own
    ///   window state.
    /// </summary>
    public event RoutedEventHandler MaximizeRequested
    {
        add { this.AddHandler(MaximizeRequestedEvent, value); }
        remove { this.RemoveHandler(MaximizeRequestedEvent, value); }
    }

    /// <summary>
    ///   Raises <see cref="CloseRequested" />.
    /// </summary>
    internal void RaiseCloseRequested() => this.RaiseEvent(new RoutedEventArgs(CloseRequestedEvent));

    /// <summary>
    ///   Raises <see cref="MaximizeRequested" />.
    /// </summary>
    internal void RaiseMaximizeRequested() => this.RaiseEvent(new RoutedEventArgs(MaximizeRequestedEvent));

    private void OnCloseClick(object sender, RoutedEventArgs e) => this.RaiseCloseRequested();

    private void OnMaximizeClick(object sender, RoutedEventArgs e) => this.RaiseMaximizeRequested();

    private void OnHeaderMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        (Window.GetWindow(this) as ChromeWindow)?.BeginHeaderDrag(e);
    }
}
