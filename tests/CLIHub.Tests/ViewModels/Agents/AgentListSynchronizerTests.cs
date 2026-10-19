namespace CLIHub.ViewModels;

using System.Collections.ObjectModel;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;

public class AgentListSynchronizerTests
{
    [Fact]
    public void Synchronize_ExistingRows_PreservesIdentityAndUpdatesProperties()
    {
        var first = Plugin("first", "First", "first.png");
        var rows = new ObservableCollection<AgentItem> { Row(first, isAvailable: true) };
        var existing = rows[0];
        var refreshed = Plugin("first", "Renamed", "renamed.png");

        var selected = AgentListSynchronizer.Synchronize(
            rows,
            [new AgentListEntry(refreshed, false)],
            "first");

        Assert.Same(existing, rows[0]);
        Assert.Same(existing, selected);
        Assert.Same(refreshed, existing.Plugin);
        Assert.Equal("Renamed", existing.Name);
        Assert.Equal("renamed.png", existing.LogoPath);
        Assert.False(existing.IsAvailable);
    }

    [Fact]
    public void Synchronize_AddsAndRemovesRows()
    {
        var retained = Plugin("retained");
        var removed = Plugin("removed");
        var added = Plugin("added");
        var rows = new ObservableCollection<AgentItem> { Row(retained), Row(removed) };

        AgentListSynchronizer.Synchronize(
            rows,
            [new AgentListEntry(retained, true), new AgentListEntry(added, true)],
            null);

        Assert.Equal(["retained", "added"], rows.Select(row => row.Plugin.Id));
        Assert.DoesNotContain(rows, row => row.Plugin.Id == "removed");
    }

    [Fact]
    public void Synchronize_AppliesTargetOrder()
    {
        var first = Plugin("first");
        var second = Plugin("second");
        var rows = new ObservableCollection<AgentItem> { Row(first), Row(second) };

        AgentListSynchronizer.Synchronize(
            rows,
            [new AgentListEntry(second, true), new AgentListEntry(first, true)],
            null);

        Assert.Equal(["second", "first"], rows.Select(row => row.Plugin.Id));
    }

    [Fact]
    public void Synchronize_SelectedRowDisappears_ClearsSelection()
    {
        var first = Plugin("first");
        var rows = new ObservableCollection<AgentItem> { Row(first) };

        var selected = AgentListSynchronizer.Synchronize(rows, [], "first");

        Assert.Null(selected);
        Assert.Empty(rows);
    }

    [Fact]
    public void Synchronize_PluginLogoPathIsFinal_NoConsumerFallbackApplied()
    {
        var logoless = Plugin("logoless");
        var rows = new ObservableCollection<AgentItem>();

        AgentListSynchronizer.Synchronize(rows, [new AgentListEntry(logoless, true)], null);

        // Rows take the plugin's logo path as final: the resolver owns the default fallback.
        Assert.Null(rows[0].LogoPath);
    }

    private static AgentItem Row(Plugin plugin, bool isAvailable = true) => new()
    {
        Plugin = plugin,
        Name = plugin.Name,
        LogoPath = plugin.LogoPath,
        IsAvailable = isAvailable
    };

    private static Plugin Plugin(string id, string? name = null, string? logoPath = null) => new()
    {
        Id = id,
        Name = name ?? id,
        LogoPath = logoPath,
        Detection = new AgentDetection()
    };
}
