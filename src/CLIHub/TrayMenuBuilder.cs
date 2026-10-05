namespace CLIHub;

using System.Windows;
using System.Windows.Controls;

using Microsoft.Win32;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Core.Updates;
using CLIHub.Views;

/// <summary>
///   Builds the system tray context menu from current application state.
/// </summary>
public sealed class TrayMenuBuilder
{
    private readonly IProjectService _projects;
    private readonly IPluginManager _pluginManager;
    private readonly IAgentCommandWorkflow _agentCommandWorkflow;
    private readonly IUpdateService _updates;
    private readonly LaunchWindow _launchWindow;
    private readonly ISettingsLauncher _settingsLauncher;
    private readonly IReleaseNotesLauncher _releaseNotesLauncher;
    private readonly IApplicationLifetime _applicationLifetime;

    /// <summary>
    ///   Raised when the user clicks the tray's download-and-restart update action.
    /// </summary>
    public event EventHandler? UpdateDownloadRequested;

    /// <summary>
    ///   Creates a tray menu builder.
    /// </summary>
    /// <param name="projects"> Project service used by the project menu. </param>
    /// <param name="pluginManager"> Plugin manager used by the agent menu. </param>
    /// <param name="agentCommandWorkflow"> Workflow invoked by the agent menu actions. </param>
    /// <param name="updates"> Update service driving the update menu item. </param>
    /// <param name="launchWindow"> The launch window shown by menu actions. </param>
    /// <param name="settingsLauncher"> Settings window launcher. </param>
    /// <param name="releaseNotesLauncher"> What's New window launcher. </param>
    /// <param name="applicationLifetime"> Application lifetime control used by the Exit action. </param>
    public TrayMenuBuilder(
        IProjectService projects,
        IPluginManager pluginManager,
        IAgentCommandWorkflow agentCommandWorkflow,
        IUpdateService updates,
        LaunchWindow launchWindow,
        ISettingsLauncher settingsLauncher,
        IReleaseNotesLauncher releaseNotesLauncher,
        IApplicationLifetime applicationLifetime)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _pluginManager = pluginManager ?? throw new ArgumentNullException(nameof(pluginManager));
        _agentCommandWorkflow = agentCommandWorkflow ?? throw new ArgumentNullException(nameof(agentCommandWorkflow));
        _updates = updates ?? throw new ArgumentNullException(nameof(updates));
        _launchWindow = launchWindow ?? throw new ArgumentNullException(nameof(launchWindow));
        _settingsLauncher = settingsLauncher ?? throw new ArgumentNullException(nameof(settingsLauncher));
        _releaseNotesLauncher = releaseNotesLauncher ?? throw new ArgumentNullException(nameof(releaseNotesLauncher));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    /// <summary>
    ///   Builds a new tray context menu from the current projects and update state.
    /// </summary>
    /// <returns> A newly constructed tray context menu. </returns>
    public ContextMenu Build()
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
        settingsItem.Click += (_, _) => _settingsLauncher.ShowSettings();
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
        showItem.Click += (_, _) => _launchWindow.ShowOnPointerMonitor();
        menu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => _applicationLifetime.Shutdown();
        menu.Items.Add(exitItem);

        return menu;
    }

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
                var result = await _agentCommandWorkflow.ExecuteAsync(plugin, current, AgentCommandKind.Launch);
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
}
