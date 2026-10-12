namespace CLIHub;

using System.Windows;
using System.Windows.Media.Imaging;

using CLIHub.Views;

using H.NotifyIcon;

/// <summary>
///   Owns the system tray icon, its context menu, and main-window visibility.
/// </summary>
public sealed class TrayIconController : ITrayHost, IDisposable
{
    private readonly ITrayActions actions;
    private readonly TrayMenuBuilder menuBuilder;
    private readonly LaunchWindow launchWindow;
    private readonly TaskbarIcon taskbarIcon;

    /// <summary>
    ///   Raised when the user clicks the tray's download-and-restart update action.
    /// </summary>
    public event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Creates the tray icon controller and builds its menu.
    /// </summary>
    /// <param name="actions"> Application actions and menu state provider. </param>
    /// <param name="menuBuilder"> Builder for the tray context menu. </param>
    /// <param name="launchWindow"> The launch window the tray toggles. </param>
    public TrayIconController(
        ITrayActions actions,
        TrayMenuBuilder menuBuilder,
        LaunchWindow launchWindow)
    {
        this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
        this.menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));
        this.launchWindow = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));

        this.actions.StateChanged += this.OnStateChanged;
        this.actions.UpdateDownloadRequested += this.OnUpdateDownloadRequested;

        this.taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        this.taskbarIcon.TrayLeftMouseUp += (_, _) => this.ShowLaunchWindow();

        this.RefreshMenu();
        this.taskbarIcon.ForceCreate();
    }

    /// <summary>
    ///   Shows and activates the launch window on the pointer's monitor.
    /// </summary>
    public void ShowLaunchWindow()
    {
        this.launchWindow.ShowOnPointerMonitor();
    }

    /// <summary>
    ///   Shows the launch window when hidden; hides it when visible.
    /// </summary>
    public void ToggleLaunchWindow()
    {
        if (this.launchWindow.IsVisible)
        {
            this.launchWindow.Hide();
        }
        else
        {
            this.ShowLaunchWindow();
        }
    }

    /// <summary>
    ///   Shows a tray notification that a new version is available.
    /// </summary>
    public void NotifyUpdateAvailable(string version)
    {
        try
        {
            this.taskbarIcon.ShowNotification("Update available", $"CLIHub {version} is available.");
        }
        catch
        {
            // Notifications can be disabled by the OS; ignore failures.
        }
    }

    /// <summary>
    ///   Shows a tray notification that an update was downloaded and the restart begins.
    /// </summary>
    public void NotifyUpdateDownloaded(string version)
    {
        try
        {
            this.taskbarIcon.ShowNotification("Update downloaded", $"CLIHub {version} was downloaded. Restarting…");
        }
        catch
        {
            // Notifications can be disabled by the OS; ignore failures.
        }
    }

    /// <summary>
    ///   Shows a tray notification that an update download failed.
    /// </summary>
    public void NotifyUpdateFailed(string version)
    {
        try
        {
            this.taskbarIcon.ShowNotification(
                "Update failed",
                $"Downloading CLIHub {version} failed. The current version keeps running.");
        }
        catch
        {
            // Notifications can be disabled by the OS; ignore failures.
        }
    }

    /// <summary>
    ///   Rebuilds the tray context menu from the current projects and update state.
    /// </summary>
    public void RefreshMenu()
    {
        var commands = this.actions.Commands with
        {
            RequestUpdateDownload = () => UpdateDownloadRequested?.Invoke(this, EventArgs.Empty),
            ShowLaunchWindow = this.ShowLaunchWindow,
            Exit = () => Application.Current?.Shutdown()
        };
        this.taskbarIcon.ContextMenu = this.menuBuilder.Build(this.actions.GetState(), commands);
    }

    /// <summary>
    ///   Disposes the tray icon.
    /// </summary>
    public void Dispose()
    {
        this.actions.StateChanged -= this.OnStateChanged;
        this.actions.UpdateDownloadRequested -= this.OnUpdateDownloadRequested;
        this.taskbarIcon.Dispose();
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            _ = dispatcher.BeginInvoke(this.RefreshMenu);
            return;
        }

        this.RefreshMenu();
    }

    private void OnUpdateDownloadRequested(object? sender, EventArgs e) =>
        UpdateDownloadRequested?.Invoke(this, e);
}

