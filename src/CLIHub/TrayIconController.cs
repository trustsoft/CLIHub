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
    private readonly ITrayActions _actions;
    private readonly TrayMenuBuilder _menuBuilder;
    private readonly LaunchWindow _launchWindow;
    private readonly TaskbarIcon _taskbarIcon;

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
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));
        _launchWindow = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));

        _actions.StateChanged += OnStateChanged;
        _actions.UpdateDownloadRequested += OnUpdateDownloadRequested;

        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => ShowLaunchWindow();

        RefreshMenu();
        _taskbarIcon.ForceCreate();
    }

    /// <summary>
    ///   Shows and activates the launch window on the pointer's monitor.
    /// </summary>
    public void ShowLaunchWindow()
    {
        _launchWindow.ShowOnPointerMonitor();
    }

    /// <summary>
    ///   Shows the launch window when hidden; hides it when visible.
    /// </summary>
    public void ToggleLaunchWindow()
    {
        if (_launchWindow.IsVisible)
        {
            _launchWindow.Hide();
        }
        else
        {
            ShowLaunchWindow();
        }
    }

    /// <summary>
    ///   Shows a tray notification that a new version is available.
    /// </summary>
    public void NotifyUpdateAvailable(string version)
    {
        try
        {
            _taskbarIcon.ShowNotification("Update available", $"CLIHub {version} is available.");
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
            _taskbarIcon.ShowNotification("Update downloaded", $"CLIHub {version} was downloaded. Restarting…");
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
            _taskbarIcon.ShowNotification(
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
        var commands = _actions.Commands with
        {
            RequestUpdateDownload = () => UpdateDownloadRequested?.Invoke(this, EventArgs.Empty),
            ShowLaunchWindow = ShowLaunchWindow,
            Exit = () => Application.Current?.Shutdown()
        };
        _taskbarIcon.ContextMenu = _menuBuilder.Build(_actions.GetState(), commands);
    }

    /// <summary>
    ///   Disposes the tray icon.
    /// </summary>
    public void Dispose()
    {
        _actions.StateChanged -= OnStateChanged;
        _actions.UpdateDownloadRequested -= OnUpdateDownloadRequested;
        _taskbarIcon.Dispose();
    }

    private void OnStateChanged(object? sender, EventArgs e) => RefreshMenu();

    private void OnUpdateDownloadRequested(object? sender, EventArgs e) =>
        UpdateDownloadRequested?.Invoke(this, e);
}

