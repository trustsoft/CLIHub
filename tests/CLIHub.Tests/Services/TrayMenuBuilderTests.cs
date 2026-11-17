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

            Assert.Equal("No project selected", (string)((MenuItem)menu.Items[0]).Header);
            Assert.Equal("first", (string)((MenuItem)menu.Items[1]).Header);
            Assert.Equal("second", (string)((MenuItem)menu.Items[2]).Header);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[3]).Header);
        });
    }

    [Fact]
    public void Build_NoFavoriteProjects_AddsDisabledNoFavoriteProjectsItem()
    {
        TestHelpers.RunOnSta(() =>
        {
            var menu = new TrayMenuBuilder().Build(CreateState([CreateProject("plain")], []), CreateCommands());

            var item = Assert.IsType<MenuItem>(menu.Items[1]);
            Assert.Equal("(no favorite projects)", (string)item.Header);
            Assert.False(item.IsEnabled);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[2]).Header);
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            var submenu = (MenuItem)menu.Items[1];
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

            Assert.Equal(7, menu.Items.Count);
            Assert.Equal("Add Project...", (string)((MenuItem)menu.Items[2]).Header);
            Assert.Equal("Settings", (string)((MenuItem)menu.Items[3]).Header);
            Assert.Equal("Updates", (string)((MenuItem)menu.Items[4]).Header);
            Assert.IsType<Separator>(menu.Items[5]);
            Assert.Equal("Exit", (string)((MenuItem)menu.Items[6]).Header);
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
            var updatesMenu = (MenuItem)menu.Items[4];

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
            var idleMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: idle.Object), CreateCommands());
            var idleItem = (MenuItem)((MenuItem)idleMenu.Items[4]).Items[0];
            Assert.Equal("Check for updates", (string)idleItem.Header);
            Assert.True(idleItem.IsEnabled);
            AssertGlyph(idleItem.Icon, IconGlyphs.Sync);

            var checking = new Mock<IUpdateWorkflow>();
            checking.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
            checking.SetupGet(x => x.IsDownloading).Returns(false);
            checking.SetupGet(x => x.IsCheckingForUpdates).Returns(true);
            var checkingMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: checking.Object), CreateCommands());
            var checkingItem = (MenuItem)((MenuItem)checkingMenu.Items[4]).Items[0];
            Assert.Equal("Checking for updates…", (string)checkingItem.Header);
            Assert.False(checkingItem.IsEnabled);
            AssertGlyph(checkingItem.Icon, IconGlyphs.Sync);

            var downloading = new Mock<IUpdateWorkflow>();
            downloading.SetupGet(x => x.LastKnownAvailableVersion).Returns((string?)null);
            downloading.SetupGet(x => x.IsDownloading).Returns(true);
            downloading.SetupGet(x => x.IsCheckingForUpdates).Returns(false);
            var downloadingMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: downloading.Object), CreateCommands());
            var downloadingItem = (MenuItem)((MenuItem)downloadingMenu.Items[4]).Items[0];
            Assert.Equal("Downloading update…", (string)downloadingItem.Header);
            Assert.False(downloadingItem.IsEnabled);
            AssertGlyph(downloadingItem.Icon, IconGlyphs.Update);

            var available = new Mock<IUpdateWorkflow>();
            available.SetupGet(x => x.LastKnownAvailableVersion).Returns("2.0.0");
            available.SetupGet(x => x.IsDownloading).Returns(false);
            available.SetupGet(x => x.IsCheckingForUpdates).Returns(false);
            var availableMenu = new TrayMenuBuilder().Build(CreateState([project], plugins, updates: available.Object), CreateCommands());
            var availableItem = (MenuItem)((MenuItem)availableMenu.Items[4]).Items[0];
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

            var projectMenu = (MenuItem)menu.Items[1];
            AssertGlyph(projectMenu.Icon, IconGlyphs.FavoriteStar);
            var agentItem = Assert.Single(projectMenu.Items.OfType<MenuItem>());
            AssertGlyph(agentItem.Icon, IconGlyphs.Play);

            AssertGlyph(((MenuItem)menu.Items[2]).Icon, IconGlyphs.FolderAdd);
            AssertGlyph(((MenuItem)menu.Items[3]).Icon, IconGlyphs.Settings);
            var updatesMenu = (MenuItem)menu.Items[4];
            AssertGlyph(updatesMenu.Icon, IconGlyphs.Sync);
            AssertGlyph(((MenuItem)updatesMenu.Items[1]).Icon, IconGlyphs.Page);
            AssertGlyph(((MenuItem)menu.Items[6]).Icon, IconGlyphs.Exit);

            var noFavoritesMenu = new TrayMenuBuilder().Build(CreateState([CreateProject("plain")], []), CreateCommands());
            Assert.Null(((MenuItem)noFavoritesMenu.Items[1]).Icon);

            var noAgentsState = CreateState([project], [CreatePlugin("nolaunch", hasLaunchCommand: false)]);
            var noAgentsMenu = new TrayMenuBuilder().Build(noAgentsState, CreateCommands());
            var noAgentsSubmenu = (MenuItem)noAgentsMenu.Items[1];
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
            var submenu = (MenuItem)menu.Items[1];
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

            Assert.Equal(7, menu.Items.Count);
            Assert.Equal("beta", (string)((MenuItem)menu.Items[1]).Header);
            var submenu = (MenuItem)menu.Items[1];
            var agentItem = Assert.Single(submenu.Items.OfType<MenuItem>());
            Assert.Equal("beta-agent", (string)agentItem.Header);
            Assert.DoesNotContain(
                menu.Items.OfType<MenuItem>(),
                item => (string)item.Header == "alpha" || (string)item.Header == "alpha-agent");
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
    /// <param name="currentProject"> Current project, or null for none. </param>
    /// <param name="installed"> Installed predicate override; all agents installed when null. </param>
    /// <param name="available"> Project-availability predicate override; all agents available when null. </param>
    /// <param name="updates"> Update workflow override with the desired update state; a fresh mock when null. </param>
    /// <returns> The projected state. </returns>
    private static TrayMenuState CreateState(
        IReadOnlyList<Project> projects,
        IReadOnlyList<Plugin> plugins,
        string? defaultAgentId = null,
        Project? currentProject = null,
        Func<Plugin, bool>? installed = null,
        Func<Plugin, bool>? available = null,
        IUpdateWorkflow? updates = null)
    {
        var projectsMock = new Mock<IProjectService>();
        projectsMock.Setup(x => x.GetCurrentProject()).Returns(currentProject);
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

        using var projection = new TrayStateProjection(
            projectsMock.Object,
            catalogMock.Object,
            updates ?? new Mock<IUpdateWorkflow>().Object,
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
