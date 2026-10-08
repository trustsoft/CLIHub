namespace CLIHub;

using System.Windows;
using System.Windows.Controls;

using CLIHub.Core.Models;

/// <summary>
///   Projects prepared tray state and commands into a WPF context menu.
/// </summary>
public sealed class TrayMenuBuilder
{
    /// <summary>
    ///   Builds a new tray context menu from prepared state and commands.
    /// </summary>
    /// <param name="state"> State to project. </param>
    /// <param name="commands"> Commands invoked by menu items. </param>
    /// <returns> A newly constructed tray context menu. </returns>
    public ContextMenu Build(TrayMenuState state, TrayMenuCommands commands)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commands);

        var menu = new ContextMenu();
        menu.Items.Add(new MenuItem
        {
            Header = state.CurrentProject != null ? $"Current: {state.CurrentProject.Name}" : "No project selected",
            IsEnabled = false
        });

        var recentMenu = new MenuItem { Header = "Recent Projects" };
        if (state.RecentProjects.Count == 0)
        {
            recentMenu.Items.Add(new MenuItem { Header = "(none)", IsEnabled = false });
        }
        else
        {
            foreach (var project in state.RecentProjects)
            {
                var id = project.Id;
                var item = new MenuItem { Header = project.IsFavorite ? $"{project.Name} *" : project.Name };
                item.Click += (_, _) => commands.SelectProject(id);
                recentMenu.Items.Add(item);
            }
        }

        menu.Items.Add(recentMenu);
        menu.Items.Add(BuildLaunchAgentMenu(state, commands));

        var addItem = new MenuItem { Header = "Add Project..." };
        addItem.Click += (_, _) => commands.AddProject();
        menu.Items.Add(addItem);

        var settingsItem = new MenuItem { Header = "Settings" };
        settingsItem.Click += (_, _) => commands.ShowSettings();
        menu.Items.Add(settingsItem);

        var whatsNewItem = new MenuItem { Header = "What's New" };
        whatsNewItem.Click += (_, _) => commands.ShowReleaseNotes();
        menu.Items.Add(whatsNewItem);

        var updateItem = BuildUpdateItem(state, commands);
        if (updateItem is not null)
        {
            menu.Items.Add(updateItem);
        }

        menu.Items.Add(new Separator());

        var showItem = new MenuItem { Header = "Show CLIHub" };
        showItem.Click += (_, _) => commands.ShowLaunchWindow();
        menu.Items.Add(showItem);

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => commands.Exit();
        menu.Items.Add(exitItem);

        return menu;
    }

    private static MenuItem? BuildUpdateItem(TrayMenuState state, TrayMenuCommands commands)
    {
        if (state.IsCheckingForUpdates)
        {
            return new MenuItem { Header = "Checking for updates…", IsEnabled = false };
        }

        if (state.AvailableUpdateVersion is null)
        {
            if (state.IsDownloadingUpdate)
            {
                return new MenuItem { Header = "Downloading update…", IsEnabled = false };
            }

            var checkItem = new MenuItem { Header = "Check for updates" };
            checkItem.Click += (_, _) => commands.CheckForUpdates();
            return checkItem;
        }

        if (state.IsDownloadingUpdate)
        {
            return new MenuItem { Header = "Downloading update…", IsEnabled = false };
        }

        var item = new MenuItem { Header = $"Download {state.AvailableUpdateVersion} and restart" };
        item.Click += (_, _) => commands.RequestUpdateDownload();
        return item;
    }

    private static MenuItem BuildLaunchAgentMenu(TrayMenuState state, TrayMenuCommands commands)
    {
        var agentsMenu = new MenuItem { Header = "Launch Agent" };
        if (state.CurrentProject is null)
        {
            agentsMenu.Items.Add(new MenuItem { Header = "(select a project)", IsEnabled = false });
            return agentsMenu;
        }

        if (state.LaunchableAgents.Count == 0)
        {
            agentsMenu.Items.Add(new MenuItem { Header = "(no agents)", IsEnabled = false });
            return agentsMenu;
        }

        foreach (var plugin in state.LaunchableAgents)
        {
            var item = new MenuItem { Header = plugin.Name };
            item.Click += async (_, _) => await commands.LaunchAgentAsync(plugin, state.CurrentProject);
            agentsMenu.Items.Add(item);
        }

        return agentsMenu;
    }
}
