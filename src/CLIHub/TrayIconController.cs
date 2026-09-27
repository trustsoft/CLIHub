using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using CLIHub.Windows;
using H.NotifyIcon;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace CLIHub;

/// <summary>
/// Owns the system tray icon, its context menu, and main-window visibility.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly IProjectService _projects;
    private readonly IPluginManager _pluginManager;
    private readonly IAgentCommandService _agentCommands;
    private readonly MainWindow _mainWindow;
    private readonly TaskbarIcon _taskbarIcon;

    public TrayIconController(
        IProjectService projects,
        IPluginManager pluginManager,
        IAgentCommandService agentCommands,
        MainWindow mainWindow)
    {
        _projects = projects;
        _pluginManager = pluginManager;
        _agentCommands = agentCommands;
        _mainWindow = mainWindow;

        _taskbarIcon = new TaskbarIcon
        {
            IconSource = new BitmapImage(new Uri("pack://application:,,,/app.ico")),
            ToolTipText = "CLIHub - AI Agent Launcher"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => ShowMainWindow();

        _projects.ProjectsChanged += (_, _) => RefreshMenu();

        RefreshMenu();
        _taskbarIcon.ForceCreate();
    }

    /// <summary>
    /// Shows and activates the main window.
    /// </summary>
    public void ShowMainWindow()
    {
        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    /// <summary>
    /// Shows the window when hidden; hides it when visible.
    /// </summary>
    public void ToggleMainWindow()
    {
        if (_mainWindow.IsVisible)
            _mainWindow.Hide();
        else
            ShowMainWindow();
    }

    /// <summary>
    /// Shows a tray notification that a new version is available.
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

    private void RefreshMenu()
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

        menu.Items.Add(new Separator());

        var showItem = new MenuItem { Header = "Show CLIHub" };
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        return menu;
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

    private void AddProject()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Project Folder"
        };

        if (dialog.ShowDialog() != true)
            return;

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

    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }
}
