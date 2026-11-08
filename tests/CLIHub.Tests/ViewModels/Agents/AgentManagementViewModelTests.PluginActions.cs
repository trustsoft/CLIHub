namespace CLIHub.Tests.ViewModels;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Tests.Builders;
using CLIHub.Theming;

using Moq;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void PluginActions_WhenBuilt_ExposesTheFourCatalogActionsInOrderWithTheirCommands()
    {
        var viewModel = new AgentManagementViewModelBuilder().Build();

        var entries = viewModel.PluginActions;

        Assert.Equal(4, entries.Count);

        Assert.Equal("Install Plugin…", entries[0].Label);
        Assert.Equal(IconGlyphs.Add, entries[0].Glyph);
        Assert.Same(viewModel.InstallPluginCommand, entries[0].Command);

        Assert.Equal("Browse Plugins…", entries[1].Label);
        Assert.Equal(IconGlyphs.Search, entries[1].Glyph);
        Assert.Same(viewModel.BrowsePluginsCommand, entries[1].Command);

        Assert.Equal("Open Plugins Folder", entries[2].Label);
        Assert.Equal(IconGlyphs.FolderOpen, entries[2].Glyph);
        Assert.Same(viewModel.OpenPluginsFolderCommand, entries[2].Command);

        Assert.Equal("Reload Catalog", entries[3].Label);
        Assert.Equal(IconGlyphs.SyncFolder, entries[3].Glyph);
        Assert.Same(viewModel.ReloadCatalogCommand, entries[3].Command);
    }

    [Fact]
    public void PluginActions_WhenFeedUrlIsDisabled_BrowseEntryCannotExecute()
    {
        var preferencesStore = new Mock<IPreferencesStore>();
        preferencesStore.Setup(x => x.Load()).Returns(new AppPreferences { PluginFeedUrl = "" });
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPreferencesStore(preferencesStore)
            .Build();

        var browseEntry = viewModel.PluginActions.Single(entry => entry.Label == "Browse Plugins…");

        // The menu entry surfaces the same gated command the standalone button used, so the
        // disabled feed preference keeps disabling the entry through Command.CanExecute.
        Assert.Same(viewModel.BrowsePluginsCommand, browseEntry.Command);
        Assert.False(viewModel.BrowsePluginsCommand.CanExecute(null));
    }
}
