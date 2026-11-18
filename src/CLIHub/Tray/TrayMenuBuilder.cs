namespace CLIHub.Tray;

using System.Windows.Controls;
using System.Windows.Media;

using CLIHub.Theming;

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
        this.Populate(menu, state, commands);
        return menu;
    }

    /// <summary>
    ///   Replaces the items of an existing tray context menu with the projection of the given
    ///   state; the menu instance and its event handlers are left untouched.
    /// </summary>
    /// <param name="menu"> The menu whose items are replaced. </param>
    /// <param name="state"> State to project. </param>
    /// <param name="commands"> Commands invoked by menu items. </param>
    public void Populate(ContextMenu menu, TrayMenuState state, TrayMenuCommands commands)
    {
        ArgumentNullException.ThrowIfNull(menu);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(commands);

        menu.Items.Clear();

        menu.Items.Add(new MenuItem
        {
            Header = $"Version {state.Version}",
            IsEnabled = false
        });

        menu.Items.Add(new Separator());

        foreach (var entry in state.FavoriteProjects)
        {
            var projectMenu = new MenuItem
            {
                Header = entry.Project.Name,
                Icon = CreateGlyph(IconGlyphs.FavoriteStar)
            };
            PopulateProjectSubmenu(projectMenu.Items, entry, commands);
            menu.Items.Add(projectMenu);
        }

        if (state.FavoriteProjects.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "(no favorite projects)", IsEnabled = false });
        }

        menu.Items.Add(new Separator());

        var addItem = new MenuItem { Header = "Add Project...", Icon = CreateGlyph(IconGlyphs.FolderAdd) };
        addItem.Click += (_, _) => commands.AddProject();
        menu.Items.Add(addItem);

        var settingsItem = new MenuItem { Header = "Settings", Icon = CreateGlyph(IconGlyphs.Settings) };
        settingsItem.Click += (_, _) => commands.ShowSettings();
        menu.Items.Add(settingsItem);

        menu.Items.Add(BuildUpdatesMenu(state, commands));

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit", Icon = CreateGlyph(IconGlyphs.Exit) };
        exitItem.Click += (_, _) => commands.Exit();
        menu.Items.Add(exitItem);
    }

    private static MenuItem BuildUpdatesMenu(TrayMenuState state, TrayMenuCommands commands)
    {
        var updatesMenu = new MenuItem { Header = "Updates", Icon = CreateGlyph(IconGlyphs.Sync) };
        updatesMenu.Items.Add(BuildUpdateItem(state, commands));

        var whatsNewItem = new MenuItem { Header = "What's New", Icon = CreateGlyph(IconGlyphs.Page) };
        whatsNewItem.Click += (_, _) => commands.ShowReleaseNotes();
        updatesMenu.Items.Add(whatsNewItem);

        return updatesMenu;
    }

    private static MenuItem BuildUpdateItem(TrayMenuState state, TrayMenuCommands commands)
    {
        if (state.IsCheckingForUpdates)
        {
            return new MenuItem
            {
                Header = "Checking for updates…",
                IsEnabled = false,
                Icon = CreateGlyph(IconGlyphs.Sync)
            };
        }

        if (state.AvailableUpdateVersion is null)
        {
            if (state.IsDownloadingUpdate)
            {
                return new MenuItem
                {
                    Header = "Downloading update…",
                    IsEnabled = false,
                    Icon = CreateGlyph(IconGlyphs.Update)
                };
            }

            var checkItem = new MenuItem { Header = "Check for updates", Icon = CreateGlyph(IconGlyphs.Sync) };
            checkItem.Click += (_, _) => commands.CheckForUpdates();
            return checkItem;
        }

        if (state.IsDownloadingUpdate)
        {
            return new MenuItem
            {
                Header = "Downloading update…",
                IsEnabled = false,
                Icon = CreateGlyph(IconGlyphs.Update)
            };
        }

        var item = new MenuItem
        {
            Header = $"Download {state.AvailableUpdateVersion} and restart",
            Icon = CreateGlyph(IconGlyphs.Update)
        };
        item.Click += (_, _) => commands.RequestUpdateDownload();
        return item;
    }

    private static void PopulateProjectSubmenu(ItemCollection items, TrayProjectAgents entry, TrayMenuCommands commands)
    {
        var project = entry.Project;
        var otherAgents = entry.InitializedAgents.Where(plugin => entry.DefaultAgent?.Id != plugin.Id).ToList();
        if (entry.DefaultAgent is { } defaultAgent)
        {
            var defaultItem = new MenuItem
            {
                Header = defaultAgent.Name,
                IsEnabled = defaultAgent.Commands?.Launch is not null,
                Icon = CreateGlyph(IconGlyphs.Play)
            };
            defaultItem.Click += async (_, _) => await commands.SelectProjectAndLaunchAgentAsync(project.Id, defaultAgent);
            items.Add(defaultItem);
            if (otherAgents.Count > 0)
            {
                items.Add(new Separator());
            }
        }

        foreach (var plugin in otherAgents)
        {
            var item = new MenuItem { Header = plugin.Name, Icon = CreateGlyph(IconGlyphs.Play) };
            item.Click += async (_, _) => await commands.SelectProjectAndLaunchAgentAsync(project.Id, plugin);
            items.Add(item);
        }

        if (entry.DefaultAgent is null && otherAgents.Count == 0)
        {
            items.Add(new MenuItem { Header = "(no agents)", IsEnabled = false });
        }
    }

    private static TextBlock CreateGlyph(string glyph) => new()
    {
        Text = glyph,
        // Matches Sizing.FontFamily.Icon / Sizing.FontSize.GlyphSmall; hard-coded because the tray
        // popup has no window resource scope.
        FontFamily = new FontFamily("Segoe MDL2 Assets"),
        FontSize = 14,
        IsHitTestVisible = false,
        Focusable = false
    };
}
