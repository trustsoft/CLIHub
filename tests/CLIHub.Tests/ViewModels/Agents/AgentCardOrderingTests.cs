namespace CLIHub.ViewModels;

using CLIHub.Core.Models;

/// <summary>
///   Tests for the AGENTS card presentation-order policy: installed first, not installed last,
///   catalog order preserved within each group.
/// </summary>
public sealed class AgentCardOrderingTests
{
    [Fact]
    public void InstalledFirst_EmptyList_ReturnsEmpty()
    {
        var ordered = AgentCardOrdering.InstalledFirst([]);

        Assert.Empty(ordered);
    }

    [Fact]
    public void InstalledFirst_AllInstalled_PreservesCatalogOrder()
    {
        var ordered = AgentCardOrdering.InstalledFirst(
        [
            Card("a", AgentInstallState.Installed),
            Card("b", AgentInstallState.Installed),
            Card("c", AgentInstallState.Installed)
        ]);

        Assert.Equal(new[] { "a", "b", "c" }, ordered.Select(card => card.PluginId));
    }

    [Fact]
    public void InstalledFirst_AllNotInstalled_PreservesCatalogOrder()
    {
        var ordered = AgentCardOrdering.InstalledFirst(
        [
            Card("a", AgentInstallState.NotInstalled),
            Card("b", AgentInstallState.NotInstalled),
            Card("c", AgentInstallState.NotInstalled)
        ]);

        Assert.Equal(new[] { "a", "b", "c" }, ordered.Select(card => card.PluginId));
    }

    [Fact]
    public void InstalledFirst_MixedList_InstalledBeforeNotInstalled()
    {
        var a = Card("a", AgentInstallState.NotInstalled);
        var b = Card("b", AgentInstallState.Installed);
        var c = Card("c", AgentInstallState.NotInstalled);
        var d = Card("d", AgentInstallState.Installed);

        var ordered = AgentCardOrdering.InstalledFirst([a, b, c, d]);

        Assert.Equal(new[] { "b", "d", "a", "c" }, ordered.Select(card => card.PluginId));
        Assert.Same(b, ordered[0]);
        Assert.Same(d, ordered[1]);
        Assert.Same(a, ordered[2]);
        Assert.Same(c, ordered[3]);
    }

    [Fact]
    public void InstalledFirst_MixedList_PreservesCatalogOrderWithinGroups()
    {
        var ordered = AgentCardOrdering.InstalledFirst(
        [
            Card("a", AgentInstallState.Installed),
            Card("b", AgentInstallState.NotInstalled),
            Card("c", AgentInstallState.Installed),
            Card("d", AgentInstallState.NotInstalled),
            Card("e", AgentInstallState.Installed)
        ]);

        Assert.Equal(new[] { "a", "c", "e", "b", "d" }, ordered.Select(card => card.PluginId));
    }

    [Fact]
    public void InstalledFirst_DetectingCardThatWasInstalled_StaysInInstalledGroup()
    {
        var ordered = AgentCardOrdering.InstalledFirst(
        [
            Card("a", AgentInstallState.Detecting),
            Card("b", AgentInstallState.NotInstalled)
        ]);

        Assert.Equal(new[] { "a", "b" }, ordered.Select(card => card.PluginId));
    }

    [Fact]
    public void InstalledFirst_DetectingCardThatWasNotInstalled_StaysInNotInstalledGroup()
    {
        var detecting = Card("a", AgentInstallState.NotInstalled);
        detecting.InstallState = AgentInstallState.Detecting;

        var ordered = AgentCardOrdering.InstalledFirst(
        [
            detecting,
            Card("b", AgentInstallState.Installed)
        ]);

        Assert.Equal(new[] { "b", "a" }, ordered.Select(card => card.PluginId));
    }

    private static AgentCardItem Card(string id, AgentInstallState state)
    {
        var plugin = new Plugin
        {
            Id = id,
            Name = id,
            PluginDirectory = $"C:\\plugins\\{id}",
            Commands = new AgentCommands { Launch = new PluginCommand { Executable = id } }
        };

        var card = new AgentCardItem(plugin, null, isInstalled: state != AgentInstallState.NotInstalled);
        if (state == AgentInstallState.Detecting)
        {
            card.InstallState = state;
        }

        return card;
    }
}
