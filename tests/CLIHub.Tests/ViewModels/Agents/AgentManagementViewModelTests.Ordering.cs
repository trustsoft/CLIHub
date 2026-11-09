namespace CLIHub.Tests.ViewModels;

using System.Collections.Specialized;

using CLIHub.Core.Agents;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Tests.Builders;
using CLIHub.ViewModels;

using Moq;

public partial class AgentManagementViewModelTests
{
    [Fact]
    public void Load_WithMixedInstallStates_OrdersInstalledCardsFirst()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var d = Launchable("d");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        detection.Setup(x => x.IsInstalledInSystem(c)).Returns(false);
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c, d])
            .WithDetection(detection)
            .Build();

        viewModel.Load(null);

        Assert.Equal(new[] { "b", "d", "a", "c" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void Load_WithAllInstalled_PreservesCatalogOrder()
    {
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(Detection())
            .Build();

        viewModel.Load(null);

        Assert.Equal(new[] { "a", "b", "c" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void Load_WithAllNotInstalled_PreservesCatalogOrder()
    {
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(false);
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(detection)
            .Build();

        viewModel.Load(null);

        Assert.Equal(new[] { "a", "b", "c" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void Load_WithMixedInstallStates_DefaultAgentOptionsFollowCardOrder()
    {
        // Accepted side effect: the dropdown is projected from Cards, so it follows the card
        // order (installed first) whenever the options are rebuilt.
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var d = Launchable("d");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        detection.Setup(x => x.IsInstalledInSystem(c)).Returns(false);
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c, d])
            .WithDetection(detection)
            .Build();

        viewModel.Load(null);

        Assert.Equal(
            new[] { "b", "d", "a", "c" },
            viewModel.DefaultAgentOptions.Where(option => option.Id is not null).Select(option => option.Id));
    }

    [Fact]
    public void Load_WithMixedInstallStatesAndDisabledAndInvalidEntries_OrdersCardsAndKeepsOtherSections()
    {
        var installed = Launchable("installed");
        var notInstalled = Launchable("not-installed");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(notInstalled)).Returns(false);
        var disabled = new DisabledPluginEntry(
            "disabled-agent",
            @"C:\plugins\disabled-agent",
            PluginOrigin.Installed,
            "1.0.0");
        var invalid = new InvalidPluginEntry(
            @"C:\plugins\invalid-agent",
            null,
            null,
            new PluginValidationResult(["Missing id"]));
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([notInstalled, installed])
            .WithDetection(detection)
            .WithDisabledPlugins(disabled)
            .WithInvalidPlugins(invalid)
            .Build();

        viewModel.Load(null);

        Assert.Equal(new[] { "installed", "not-installed" }, viewModel.Cards.Select(card => card.PluginId));
        var disabledCard = Assert.Single(viewModel.DisabledCards);
        Assert.Equal("disabled-agent", disabledCard.Name);
        var invalidCard = Assert.Single(viewModel.InvalidCards);
        Assert.Equal("Missing id", invalidCard.Reason);
        Assert.DoesNotContain(viewModel.Cards, card => card.PluginId == "disabled-agent");
    }

    [Fact]
    public void DetectCardCommand_WhenInstalledCardProbeReportsNotInstalled_MovesCardIntoNotInstalledGroup()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(c)).Returns(false);
        var probe = new Mock<IAgentProbeService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(false, null));
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c])
            .WithDetection(detection)
            .WithProbe(probe)
            .Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards.Single(card => card.PluginId == "a");
        Assert.Equal(new[] { "a", "b", "c" }, viewModel.Cards.Select(card => card.PluginId));

        viewModel.DetectCardCommand.Execute(cardA);

        // The not-installed group keeps catalog order, so the updated card precedes c.
        Assert.Equal(new[] { "b", "a", "c" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void DetectCardCommand_WhenNotInstalledCardProbeReportsInstalled_MovesItBeforeNotInstalledCards()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        detection.Setup(x => x.IsInstalledInSystem(c)).Returns(false);
        var probe = new Mock<IAgentProbeService>();
        probe
            .Setup(x => x.ProbeAsync(c, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(true, "2.0"));
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c])
            .WithDetection(detection)
            .WithProbe(probe)
            .Build();
        viewModel.Load(null);
        var cardC = viewModel.Cards.Single(card => card.PluginId == "c");
        Assert.Equal(new[] { "b", "a", "c" }, viewModel.Cards.Select(card => card.PluginId));

        viewModel.DetectCardCommand.Execute(cardC);

        Assert.Equal(new[] { "b", "c", "a" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void CardInstallState_WhenChangedToDetecting_KeepsCardPosition()
    {
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Returns<Plugin>(plugin => plugin.Id == "a");
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(detection)
            .Build();
        viewModel.Load(null);
        Assert.Equal(new[] { "a", "b", "c" }, viewModel.Cards.Select(card => card.PluginId));

        viewModel.Cards.Single(card => card.PluginId == "c").InstallState = AgentInstallState.Detecting;

        Assert.Equal(new[] { "a", "b", "c" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void CardInstallState_WhenSettlesToInstalled_MovesCardBeforeNotInstalledCards()
    {
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Returns<Plugin>(plugin => plugin.Id == "a");
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(detection)
            .Build();
        viewModel.Load(null);

        viewModel.Cards.Single(card => card.PluginId == "c").InstallState = AgentInstallState.Installed;

        Assert.Equal(new[] { "a", "c", "b" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void CardInstallState_WhenSettlesToInstalled_ReordersOnceWithoutReentry()
    {
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>()))
            .Returns<Plugin>(plugin => plugin.Id == "a");
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([Launchable("a"), Launchable("b"), Launchable("c")])
            .WithDetection(detection)
            .Build();
        viewModel.Load(null);
        var moves = 0;
        viewModel.Cards.CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                moves++;
            }
        };

        viewModel.Cards.Single(card => card.PluginId == "c").InstallState = AgentInstallState.Installed;

        Assert.Equal(1, moves);
        Assert.Equal(new[] { "a", "c", "b" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void DetectCommand_WhenACardSettlesMidPass_KeepsDetectingCardsInLastSettledGroups()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var d = Launchable("d");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        detection.Setup(x => x.IsInstalledInSystem(d)).Returns(false);
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateC = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id switch
            {
                "a" => gateA.Task,
                "c" => gateC.Task,
                _ => Task.FromResult(new AgentProbeResult(false, null))
            });
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c, d])
            .WithDetection(detection)
            .WithProbe(probe)
            .WithOperationLifetime(new DeferredOperationLifetime())
            .Build();
        viewModel.Load(null);
        Assert.Equal(new[] { "a", "c", "b", "d" }, viewModel.Cards.Select(card => card.PluginId));

        viewModel.DetectCommand.Execute(null);

        // Every card is still announcing Detecting; settling a as missing moves only a into the
        // not-installed group, ahead of the still-detecting, previously-missing b and d.
        gateA.SetResult(new AgentProbeResult(false, null));
        Assert.True(SpinWait.SpinUntil(
            () => viewModel.Cards.Count == 4 && viewModel.Cards[0].PluginId == "c",
            TimeSpan.FromSeconds(5)));
        Assert.Equal(new[] { "c", "a", "b", "d" }, viewModel.Cards.Select(card => card.PluginId));

        gateC.SetResult(new AgentProbeResult(true, "1.0"));
        Assert.True(SpinWait.SpinUntil(() => !viewModel.IsDetecting, TimeSpan.FromSeconds(5)));

        Assert.Equal(new[] { "c", "a", "b", "d" }, viewModel.Cards.Select(card => card.PluginId));
    }

    [Fact]
    public void DetectCommand_WhenPassSettlesMixedStates_OrdersInstalledFirst()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var c = Launchable("c");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var probe = new Mock<IAgentProbeService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => Task.FromResult(plugin.Id switch
            {
                "a" => new AgentProbeResult(false, null),
                "c" => new AgentProbeResult(true, "1.0"),
                _ => new AgentProbeResult(true, "2.0")
            }));
        var viewModel = new AgentManagementViewModelBuilder()
            .WithPlugins([a, b, c])
            .WithDetection(detection)
            .WithProbe(probe)
            .Build();
        viewModel.Load(null);
        Assert.Equal(new[] { "a", "c", "b" }, viewModel.Cards.Select(card => card.PluginId));

        viewModel.DetectCommand.Execute(null);

        // Both settled installed cards are listed in catalog order (b precedes c), then the
        // not-installed card.
        Assert.Equal(new[] { "b", "c", "a" }, viewModel.Cards.Select(card => card.PluginId));
    }
}
