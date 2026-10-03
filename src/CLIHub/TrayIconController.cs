namespace CLIHub;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.Windows;
using H.NotifyIcon;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

/// <summary>
///   Owns the system tray icon, its context menu, and main-window visibility.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly IProjectService _projects;
    private readonly IPluginManager _pluginManager;
    private readonly IAgentCommandService _agentCommands;
    private readonly IUpdateService _updates;
    private readonly LaunchWindow _launchWindow;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private readonly TaskbarIcon _taskbarIcon;

    /// <summary>
    ///   Raised when the user clicks the tray's download-and-restart update action.
    /// </summary>
    public event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Creates the tray icon controller and builds its menu.
    /// </summary>
    /// <param name="projects"> Project service used by the project menu. </param>
    /// <param name="pluginManager"> Plugin manager used by the agent menu. </param>
    /// <param name="agentCommands"> Service invoked by the agent menu actions. </param>
    /// <param name="updates"> Update service driving the update menu item. </param>
    /// <param name="launchWindow"> The launch window the tray toggles. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="releaseNotesLauncher"> What's New window launcher. </param>
    public TrayIconController(
        IProjectService projects,
        IPluginManager pluginManager,
        IAgentCommandService agentCommands,
        IUpdateService updates,
        LaunchWindow launchWindow,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher)
    {
        _projects = projects;
        _pluginManager = pluginManager;
        _agentCommands = agentCommands;
        _updates = updates;
        _launchWindow = launchWindow;
        _settingsLauncher = settingsLauncher;
        _releaseNotesLauncher = releaseNotesLauncher;

        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => ShowLaunchWindow();

        _projects.ProjectsChanged += (_, _) => RefreshMenu();

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
        _taskbarIcon.ContextMenu = BuildContextMenu();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();

        var current = _projects.GetCurrentProject();
        menu.Items.Add(new MenuItem
        {
            Header = current != null ? $"Current: {current.Name}" : "No project selected",
            IsEnabled = false
        });

        var recentMenu = new MenuItem { Header = "Recent Projects" };
        var recent = _projects.GetRecentProjects(10);
        if (recent.Count == 0)
        {
            recentMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
        }
        else
        {
            foreach (var project in recent)
            {
                var item = new MenuItem
                {
                    Header = project.IsFavorite ? $"{project.Name} *" : project.Name
                };
                var id = project.Id;
                item.Click += (_, _) => _projects.SetCurrentProject(id);
                recentMenu.Items.Add(item);
            }
        }
        menu.Items.Add(recentMenu);

        menu.Items.Add(BuildLaunchAgentMenu());

        var addItem = new MenuItem { Header = "Add Project..." };
        addItem.Click += (_, _) => AddProject();
        menu.Items.Add(addItem);

        var settingsItem = new MenuItem { Header = "Settings" };
        settingsItem.Click += (_, _) => ShowSettings();
        menu.Items.Add(settingsItem);

        var whatsNewItem = new MenuItem { Header = "What's New" };
        whatsNewItem.Click += (_, _) => _releaseNotesLauncher.ShowReleaseNotes();
        menu.Items.Add(whatsNewItem);

        var updateItem = BuildUpdateItem();
        if (updateItem != null)
        {
            menu.Items.Add(updateItem);
        }

        menu.Items.Add(new Separator());

        var showItem = new MenuItem { Header = "Show CLIHub" };
        showItem.Click += (_, _) => ShowLaunchWindow();
        menu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    ///   Builds the update menu item: a download-and-restart action when an update is available,
    ///   a disabled downloading marker while the download runs, and nothing without an update.
    /// </summary>
    private MenuItem? BuildUpdateItem()
    {
        var version = _updates.LastKnownAvailableVersion;

        if (version == null)
        {
            return null;
        }

        if (_updates.IsDownloading)
        {
            return new MenuItem { Header = "Downloading update…", IsEnabled = false };
        }

        var item = new MenuItem { Header = $"Download {version} and restart" };
        item.Click += (_, _) => UpdateDownloadRequested?.Invoke(this, EventArgs.Empty);
        return item;
    }

    private MenuItem BuildLaunchAgentMenu()
    {
        var agentsMenu = new MenuItem { Header = "Launch Agent" };

        var current = _projects.GetCurrentProject();
        if (current == null)
        {
            agentsMenu.Items.Add(new MenuItem { Header = "(select a project)", IsEnabled = false });
            return agentsMenu;
        }

        var launchable = _pluginManager.GetAllPlugins()
            .Where(p => p.Commands?.Launch != null)
            .ToList();

        if (launchable.Count == 0)
        {
            agentsMenu.Items.Add(new MenuItem { Header = "(no agents)", IsEnabled = false });
            return agentsMenu;
        }

        foreach (var plugin in launchable)
        {
            var item = new MenuItem { Header = plugin.Name };
            item.Click += async (_, _) =>
            {
                var result = await _agentCommands.ExecuteAsync(plugin, AgentCommandKind.Launch, current.Path);
                if (!result.Success)
                {
                    MessageBox.Show(
                        result.Error ?? $"Failed to launch {plugin.Name}",
                        "CLIHub",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            };
            agentsMenu.Items.Add(item);
        }

        return agentsMenu;
    }

    private void ShowSettings()
    {
        _settingsLauncher.ShowSettings();
    }

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var project = _projects.AddProject(dialog.FolderName);
            _projects.SetCurrentProject(project.Id);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not add project: {ex.Message}",
                "CLIHub",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    /// <summary>
    ///   Disposes the tray icon.
    /// </summary>
    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }
}
