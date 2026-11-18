namespace CLIHub.Tests.Services;

using System.Windows;
using System.Windows.Controls;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Projects;
using CLIHub.Tests.Support;
using CLIHub.Theming;
using CLIHub.Tray;
using CLIHub.Updates;

using Moq;

public class TrayMenuBuilderTests
{
    [Fact]
    public void Build_OnlyFavoriteProjects_AddsProjectSubmenusInStorageOrder()
    {
        TestHelpers.RunOnSta(() =>
        {
            var first = CreateProject("first", isFavorite: true);
            var nonFavorite = CreateProject("plain");
            var second = CreateProject("second", isFavorite: true);
            var menu = new TrayMenuBuilder().Build(CreateState([first, nonFavorite, second], []), CreateCommands());

            Assert.Equal("Version 1.2.1", (string)((MenuItem)menu.Items[0]).Header);
            Assert.Equal("first", (string)((MenuItem)menu.Items[2]).Header);
            Assert.Equal("second", (string)((MenuItem)menu.Items[3]).Header);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[5]).Header);
        });
    }

    [Fact]
    public void Build_NoFavoriteProjects_AddsDisabledNoFavoriteProjectsItem()
    {
        TestHelpers.RunOnSta(() =>
        {
            var menu = new TrayMenuBuilder().Build(CreateState([CreateProject("plain")], []), CreateCommands());

            Assert.IsType<Separator>(menu.Items[1]);
            var item = Assert.IsType<MenuItem>(menu.Items[2]);
            Assert.Equal("(no favorite projects)", (string)item.Header);
            Assert.False(item.IsEnabled);
            Assert.IsType<Separator>(menu.Items[3]);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[4]).Header);
        });
    }

    [Fact]
    public void Build_ProjectSubmenu_ListsOnlyInitializedAgentsWithAgentNameLabels()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[]
            {
                CreatePlugin("ready", hasLaunchCommand: true),
                CreatePlugin("uninstalled", hasLaunchCommand: true),
                CreatePlugin("unavailable", hasLaunchCommand: true),
                CreatePlugin("nolaunch", hasLaunchCommand: false)
            };
            var state = CreateState(
                [project],
                plugins,
                installed: plugin => plugin.Id != "uninstalled",
                available: plugin => plugin.Id != "unavailable");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            var agentItem = Assert.Single(submenu.Items.OfType<MenuItem>());
            Assert.Equal("ready", (string)agentItem.Header);
            Assert.True(agentItem.IsEnabled);
        });
    }

    [Fact]
    public void Build_ConfiguredDefaultAgent_IsFirstFollowedBySeparatorEvenWhenNotInitialized()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[]
            {
                CreatePlugin("default", hasLaunchCommand: true),
                CreatePlugin("other", hasLaunchCommand: true)
            };
            var state = CreateState([project], plugins, defaultAgentId: "default", installed: plugin => plugin.Id != "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Equal(3, submenu.Items.Count);
            var defaultItem = Assert.IsType<MenuItem>(submenu.Items[0]);
            Assert.Equal("default", (string)defaultItem.Header);
            Assert.True(defaultItem.IsEnabled);
            Assert.IsType<Separator>(submenu.Items[1]);
            Assert.Equal("other", (string)((MenuItem)submenu.Items[2]).Header);
        });
    }

    [Fact]
    public void Build_DefaultWithoutLaunchCommand_IsFirstAndDisabledWithSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[]
            {
                CreatePlugin("default", hasLaunchCommand: false),
                CreatePlugin("other", hasLaunchCommand: true)
            };
            var state = CreateState([project], plugins, defaultAgentId: "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Equal(3, submenu.Items.Count);
            var defaultItem = Assert.IsType<MenuItem>(submenu.Items[0]);
            Assert.Equal("default", (string)defaultItem.Header);
            Assert.False(defaultItem.IsEnabled);
            Assert.IsType<Separator>(submenu.Items[1]);
            Assert.Equal("other", (string)((MenuItem)submenu.Items[2]).Header);
            Assert.Equal(
                1,
                submenu.Items.OfType<MenuItem>().Count(item => (string)item.Header == "default"));
        });
    }

    [Fact]
    public void Build_InitializedDefaultAgent_AppearsOnceAsFirstItemWithSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[]
            {
                CreatePlugin("default", hasLaunchCommand: true),
                CreatePlugin("other", hasLaunchCommand: true)
            };
            var state = CreateState([project], plugins, defaultAgentId: "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Equal(3, submenu.Items.Count);
            Assert.Equal("default", (string)((MenuItem)submenu.Items[0]).Header);
            Assert.IsType<Separator>(submenu.Items[1]);
            Assert.Equal(
                1,
                submenu.Items.OfType<MenuItem>().Count(item => (string)item.Header == "default"));
        });
    }

    [Fact]
    public void Build_DefaultAgentOnly_ShowsSingleItemWithoutSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var state = CreateState(
                [project],
                [CreatePlugin("default", hasLaunchCommand: true)],
                defaultAgentId: "default",
                installed: plugin => plugin.Id != "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Single(submenu.Items);
            var defaultItem = Assert.IsType<MenuItem>(Assert.Single(submenu.Items));
            Assert.Equal("default", (string)defaultItem.Header);
            Assert.Empty(submenu.Items.OfType<Separator>());
        });
    }

    [Fact]
    public void Build_InitializedDefaultAgentOnly_ShowsSingleItemWithoutSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var state = CreateState(
                [project],
                [CreatePlugin("default", hasLaunchCommand: true)],
                defaultAgentId: "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Single(submenu.Items);
            var defaultItem = Assert.IsType<MenuItem>(Assert.Single(submenu.Items));
            Assert.Equal("default", (string)defaultItem.Header);
            Assert.True(defaultItem.IsEnabled);
            Assert.Empty(submenu.Items.OfType<Separator>());
        });
    }

    [Fact]
    public void Build_DefaultAgentWithAllOthersExcluded_ShowsSingleItemWithoutSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[]
            {
                CreatePlugin("default", hasLaunchCommand: true),
                CreatePlugin("other", hasLaunchCommand: true)
            };
            var state = CreateState(
                [project],
                plugins,
                defaultAgentId: "default",
                available: plugin => plugin.Id == "default");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Single(submenu.Items);
            var defaultItem = Assert.IsType<MenuItem>(Assert.Single(submenu.Items));
            Assert.Equal("default", (string)defaultItem.Header);
            Assert.Empty(submenu.Items.OfType<Separator>());
        });
    }

    [Fact]
    public void Build_NoDefaultConfigured_HasNoDefaultItemOrSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var state = CreateState([project], [CreatePlugin("agent", hasLaunchCommand: true)]);

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Empty(submenu.Items.OfType<Separator>());
            var agentItem = Assert.Single(submenu.Items.OfType<MenuItem>());
            Assert.Equal("agent", (string)agentItem.Header);
        });
    }

    [Fact]
    public void Build_DefaultIdNotInCatalog_HasNoDefaultItemOrSeparator()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var state = CreateState([project], [CreatePlugin("agent", hasLaunchCommand: true)], defaultAgentId: "missing");

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            Assert.Empty(submenu.Items.OfType<Separator>());
            var agentItem = Assert.Single(submenu.Items.OfType<MenuItem>());
            Assert.Equal("agent", (string)agentItem.Header);
        });
    }

    [Fact]
    public void Build_ProjectWithNoDefaultAndNoInitializedAgents_ShowsDisabledNoAgentsItem()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var state = CreateState([project], [CreatePlugin("nolaunch", hasLaunchCommand: false)]);

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            var submenu = (MenuItem)menu.Items[2];
            var item = Assert.IsType<MenuItem>(Assert.Single(submenu.Items));
            Assert.Equal("(no agents)", (string)item.Header);
            Assert.False(item.IsEnabled);
        });
    }

    [Fact]
    public void Build_MenuTail_KeepsAddProjectSettingsUpdatesSeparatorAndExitInOrder()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("favorite", isFavorite: true);
            var state = CreateState([project], [CreatePlugin("agent", hasLaunchCommand: true)]);

            var menu = new TrayMenuBuilder().Build(state, CreateCommands());

            Assert.Equal(9, menu.Items.Count);
            Assert.IsType<Separator>(menu.Items[1]);
            Assert.IsType<Separator>(menu.Items[3]);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[4]).Header);
            Assert.Equal("Settings", (string)((MenuItem)menu.Items[5]).Header);
            Assert.Equal("Updates", (string)((MenuItem)menu.Items[6]).Header);
            Assert.IsType<Separator>(menu.Items[7]);
            Assert.Equal("Exit", (string)((MenuItem)menu.Items[8]).Header);
            Assert.DoesNotContain(menu.Items.OfType<MenuItem>(), item => (string)item.Header == "Show CLIHub");
            Assert.DoesNotContain(menu.Items.OfType<MenuItem>(), item => (string)item.Header == "What's New");
        });
    }

    [Fact]
    public void Build_UpdatesSubmenu_ContainsUpdateItemThenWhatsNew()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("favorite", isFavorite: true);
            var state = CreateState([project], [CreatePlugin("agent", hasLaunchCommand: true)]);
            var (commands, launches, releaseNotes) = CreateRecordingCommands();

            var menu = new TrayMenuBuilder().Build(state, commands);
            var updatesMenu = (MenuItem)menu.Items[6];

            Assert.Equal(2, updatesMenu.Items.Count);
            var checkItem = Assert.IsType<MenuItem>(updatesMenu.Items[0]);
            Assert.Equal("Check for updates", (string)checkItem.Header);
            Assert.True(checkItem.IsEnabled);
            var whatsNewItem = Assert.IsType<MenuItem>(updatesMenu.Items[1]);
            Assert.Equal("What's New", (string)whatsNewItem.Header);

            whatsNewItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

            Assert.Single(releaseNotes);
            Assert.Empty(launches);
        });
    }

    [Fact]
    public void Build_UpdateItemIcons_FollowUpdateState()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("favorite", isFavorite: true);
            var plugins = new[] { CreatePlugin("agent", hasLaunchCommand: true) };

            var idle = new Mock<IUpdateWorkflow>();
            idle.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
            idle.SetupGet(x => x.IsDownloading).Returns(false);
            idle.SetupGet(x => x.IsCheckingForUpdates).Returns(false);
            idle.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");
            var idleMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: idle.Object), CreateCommands());
            var idleItem = (MenuItem)((MenuItem)idleMenu.Items[6]).Items[0];
            Assert.Equal("Check for updates", (string)idleItem.Header);
            Assert.True(idleItem.IsEnabled);
            AssertGlyph(idleItem.Icon, IconGlyphs.Sync);

            var checking = new Mock<IUpdateWorkflow>();
            checking.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
            checking.SetupGet(x => x.IsDownloading).Returns(false);
            checking.SetupGet(x => x.IsCheckingForUpdates).Returns(true);
            checking.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");
            var checkingMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: checking.Object), CreateCommands());
            var checkingItem = (MenuItem)((MenuItem)checkingMenu.Items[6]).Items[0];
            Assert.Equal("Checking for updates…", (string)checkingItem.Header);
            Assert.False(checkingItem.IsEnabled);
            AssertGlyph(checkingItem.Icon, IconGlyphs.Sync);

            var downloading = new Mock<IUpdateWorkflow>();
            downloading.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
            downloading.SetupGet(x => x.IsDownloading).Returns(true);
            downloading.SetupGet(x => x.IsCheckingForUpdates).Returns(false);
            downloading.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");
            var downloadingMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: downloading.Object), CreateCommands());
            var downloadingItem = (MenuItem)((MenuItem)downloadingMenu.Items[6]).Items[0];
            Assert.Equal("Downloading update…", (string)downloadingItem.Header);
            Assert.False(downloadingItem.IsEnabled);
            AssertGlyph(downloadingItem.Icon, IconGlyphs.Update);

            var available = new Mock<IUpdateWorkflow>();
            available.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0.0");
            available.SetupGet(x => x.IsDownloading).Returns(false);
            available.SetupGet(x => x.IsCheckingForUpdates).Returns(false);
            available.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");
            var availableMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: available.Object), CreateCommands());
            var availableItem = (MenuItem)((MenuItem)availableMenu.Items[6]).Items[0];
            Assert.Equal("Download 2.0.0 and restart", (string)availableItem.Header);
            Assert.True(availableItem.IsEnabled);
            AssertGlyph(availableItem.Icon, IconGlyphs.Update);
        });
    }

    [Fact]
    public void Build_ActionableItems_CarryMdl2Glyphs()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugins = new[] { CreatePlugin("agent", hasLaunchCommand: true) };
            var menu = new TrayMenuBuilder().Build(CreateState([project], plugins), CreateCommands());

            var header = (MenuItem)menu.Items[0];
            Assert.Null(header.Icon);

            var projectMenu = (MenuItem)menu.Items[2];
            AssertGlyph(projectMenu.Icon, IconGlyphs.FavoriteStar);
            var agentItem = Assert.Single(projectMenu.Items.OfType<MenuItem>());
            AssertGlyph(agentItem.Icon, IconGlyphs.Play);

            AssertGlyph(((MenuItem)menu.Items[4]).Icon, IconGlyphs.FolderAdd);
            AssertGlyph(((MenuItem)menu.Items[5]).Icon, IconGlyphs.Settings);
            var updatesMenu = (MenuItem)menu.Items[6];
            AssertGlyph(updatesMenu.Icon, IconGlyphs.Sync);
            AssertGlyph(((MenuItem)updatesMenu.Items[1]).Icon, IconGlyphs.Page);
            AssertGlyph(((MenuItem)menu.Items[8]).Icon, IconGlyphs.Exit);

            var noFavoritesMenu = new TrayMenuBuilder().Build(CreateState([CreateProject("plain")], []), CreateCommands());
            Assert.Null(((MenuItem)noFavoritesMenu.Items[2]).Icon);

            var noAgentsState = CreateState([project], [CreatePlugin("nolaunch", hasLaunchCommand: false)]);
            var noAgentsMenu = new TrayMenuBuilder().Build(noAgentsState, CreateCommands());
            var noAgentsSubmenu = (MenuItem)noAgentsMenu.Items[2];
            var noAgentsItem = Assert.IsType<MenuItem>(Assert.Single(noAgentsSubmenu.Items));
            Assert.Null(noAgentsItem.Icon);
        });
    }

    [Fact]
    public void InvokeInitializedAgent_InvokesSelectProjectAndLaunch()
    {
        TestHelpers.RunOnSta(() =>
        {
            var project = CreateProject("project", isFavorite: true);
            var plugin = CreatePlugin("agent", hasLaunchCommand: true);
            var state = CreateState([project], [plugin]);
            var (commands, launches, _) = CreateRecordingCommands();

            var menu = new TrayMenuBuilder().Build(state, commands);
            var submenu = (MenuItem)menu.Items[2];
            var agentItem = submenu.Items.OfType<MenuItem>().Single(item => (string)item.Header == "agent");

            agentItem.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

            var launch = Assert.Single(launches);
            Assert.Equal(("project", "agent"), (launch.ProjectId, launch.Agent.Id));
            Assert.Same(plugin, launch.Agent);
        });
    }

    [Fact]
    public void Populate_ExistingMenu_ReplacesItemsWithFreshState()
    {
        TestHelpers.RunOnSta(() =>
        {
            var projectA = CreateProject("alpha", isFavorite: true);
            var projectB = CreateProject("beta", isFavorite: true);
            var pluginA = CreatePlugin("alpha-agent", hasLaunchCommand: true);
            var pluginB = CreatePlugin("beta-agent", hasLaunchCommand: true);
            var builder = new TrayMenuBuilder();
            var commands = CreateCommands();

            var menu = builder.Build(CreateState([projectA], [pluginA]), commands);
            builder.Populate(menu, CreateState([projectB], [pluginB]), commands);

            Assert.Equal(9, menu.Items.Count);
            Assert.Equal("beta", (string)((MenuItem)menu.Items[2]).Header);
            var submenu = (MenuItem)menu.Items[2];
            var agentItem = Assert.Single(submenu.Items.OfType<MenuItem>());
            Assert.Equal("beta-agent", (string)agentItem.Header);
            Assert.DoesNotContain(
                menu.Items.OfType<MenuItem>(),
                item => (string)item.Header == "alpha" || (string)item.Header == "alpha-agent");
        });
    }

    [Fact]
    public void Build_Separators_DivideHeaderProjectListAndExit()
    {
        TestHelpers.RunOnSta(() =>
        {
            var favorite = CreateProject("favorite", isFavorite: true);
            var favoritesMenu = new TrayMenuBuilder().Build(CreateState([favorite], []), CreateCommands());

            Assert.Equal("Version 1.2.1", (string)((MenuItem)favoritesMenu.Items[0]).Header);
            Assert.False(((MenuItem)favoritesMenu.Items[0]).IsEnabled);
            Assert.Null(((MenuItem)favoritesMenu.Items[0]).Icon);
            Assert.IsType<Separator>(favoritesMenu.Items[1]);
            Assert.Equal("favorite", (string)((MenuItem)favoritesMenu.Items[2]).Header);
            Assert.IsType<Separator>(favoritesMenu.Items[3]);
            Assert.Equal("Add Project...", (string)((MenuItem)favoritesMenu.Items[4]).Header);
            Assert.IsType<Separator>(favoritesMenu.Items[7]);
            Assert.Equal("Exit", (string)((MenuItem)favoritesMenu.Items[8]).Header);
            Assert.Equal(3, favoritesMenu.Items.OfType<Separator>().Count());

            var noFavoritesMenu = new TrayMenuBuilder().Build(CreateState([CreateProject("plain")], []), CreateCommands());

            Assert.False(((MenuItem)noFavoritesMenu.Items[0]).IsEnabled);
            Assert.Null(((MenuItem)noFavoritesMenu.Items[0]).Icon);
            Assert.IsType<Separator>(noFavoritesMenu.Items[1]);
            var placeholder = Assert.IsType<MenuItem>(noFavoritesMenu.Items[2]);
            Assert.Equal("(no favorite projects)", (string)placeholder.Header);
            Assert.IsType<Separator>(noFavoritesMenu.Items[3]);
            Assert.Equal("Add Project...", (string)((MenuItem)noFavoritesMenu.Items[4]).Header);
            Assert.IsType<Separator>(noFavoritesMenu.Items[7]);
            Assert.Equal("Exit", (string)((MenuItem)noFavoritesMenu.Items[8]).Header);
            Assert.Equal(3, noFavoritesMenu.Items.OfType<Separator>().Count());
        });
    }

    /// <summary>
    ///   Asserts that an item's icon is an MDL2 glyph <see cref="TextBlock" /> carrying the
    ///   expected glyph.
    /// </summary>
    /// <param name="icon"> The icon to check. </param>
    /// <param name="expectedGlyph"> The expected <see cref="IconGlyphs" /> constant. </param>
    private static void AssertGlyph(object? icon, string expectedGlyph)
    {
        var glyph = Assert.IsType<TextBlock>(icon);
        Assert.Equal("Segoe MDL2 Assets", glyph.FontFamily.Source);
        Assert.Equal(14, (int)glyph.FontSize);
        Assert.Equal(expectedGlyph, glyph.Text);
    }

    /// <summary>
    ///   Creates menu commands whose launch and release-notes slots record invocations.
    /// </summary>
    /// <returns> The commands, the recorded project/agent pairs, and the release-notes invocations. </returns>
    private static (TrayMenuCommands Commands,
                    List<(string ProjectId, Plugin Agent)> Launches,
                    List<object> ReleaseNotes) CreateRecordingCommands()
    {
        var launches = new List<(string ProjectId, Plugin Agent)>();
        var releaseNotes = new List<object>();
        var commands = new TrayMenuCommands(
            (projectId, plugin) =>
            {
                launches.Add((projectId, plugin));
                return Task.CompletedTask;
            },
            () => { },
            () => { },
            () => releaseNotes.Add(new object()),
            () => { },
            () => { },
            () => { });
        return (commands, launches, releaseNotes);
    }

    private static TrayMenuCommands CreateCommands() => CreateRecordingCommands().Commands;

    /// <summary>
    ///   Builds the tray menu state through the real projection over mocked sources.
    /// </summary>
    /// <param name="projects"> Registered projects. </param>
    /// <param name="plugins"> Catalog plugins. </param>
    /// <param name="defaultAgentId"> Configured default agent id, or null for none. </param>
    /// <param name="installed"> Installed predicate override; all agents installed when null. </param>
    /// <param name="available"> Project-availability predicate override; all agents available when null. </param>
    /// <param name="updates"> Update workflow override with the desired update state; a fresh mock when null. </param>
    /// <returns> The projected state. </returns>
    private static TrayMenuState CreateState(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Plugin> plugins,
        string? defaultAgentId = null,
        Func<Plugin, bool>? installed = null,
        Func<Plugin, bool>? available = null,
        IUpdateWorkflow? updates = null)
    {
        var projectsMock = new Mock<IProjectService>();
        projectsMock.Setup(x => x.GetAllProjects()).Returns(projects);
        var catalogMock = new Mock<IPluginCatalog>();
        catalogMock.Setup(x => x.GetAllPlugins()).Returns(plugins);
        var detectionMock = new Mock<IAgentDetectionService>();
        detectionMock
            .Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Returns((Plugin plugin) => installed?.Invoke(plugin) ?? true);
        detectionMock
            .Setup(x => x.IsAvailableInProject(It.IsAny<Plugin>(), It.IsAny<string>()))
            .Returns((Plugin plugin, string projectPath) => available?.Invoke(plugin) ?? true);
        var preferencesMock = new Mock<IPreferencesStore>();
        preferencesMock.Setup(x => x.Load()).Returns(new AppPreferences { DefaultAgentId = defaultAgentId });
        var fallbackUpdates = new Mock<IUpdateWorkflow>();
        fallbackUpdates.Setup(x => x.GetCurrentVersion()).Returns("1.2.1");

        using var projection = new TrayStateProjection(
            projectsMock.Object,
            catalogMock.Object,
            updates ?? fallbackUpdates.Object,
            detectionMock.Object,
            preferencesMock.Object);
        return projection.GetState();
    }

    private static Project CreateProject(string id, bool isFavorite = false) => new()
    {
        Id = id,
        Name = id,
        Path = $"C:\\{id}",
        IsFavorite = isFavorite
    };

    private static Plugin CreatePlugin(string id, bool hasLaunchCommand) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands
        {
            Launch = hasLaunchCommand ? new PluginCommand { Executable = id } : null
        }
    };
}
