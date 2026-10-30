namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.ViewModels;
using CLIHub.Lifetime;
using CLIHub.Launching;
using CLIHub.Tests.Builders;

using static CLIHub.Tests.Support.TestHelpers;

public partial class AgentManagementViewModelTests
{

    [Fact]
    public void DetectCardCommand_UpdatesInstallationAndVersionWithoutAvailabilityCheck()
    {
        var a = Launchable("a");
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "9.9"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);

        // Pre-seed a stale update indicator; the per-card Detect re-probes installation and
        // version only, so the indicator must survive untouched.
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.9.0", "1.0.0");

        viewModel.DetectCardCommand.Execute(card);

        Assert.True(card.IsInstalled);
        Assert.Equal("9.9", card.Version);
        Assert.Equal("Installed · version 9.9", card.Message);
        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("0.9.0 → 1.0.0", card.UpdateAvailabilityText);
        Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);

        // The Load pass checked once; the per-card Detect settle added no availability check.
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public void DetectCommand_UpdatesCardsSummaryAndOptionAvailability()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "2.0"));
        probe.Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);
        Assert.Equal("2 supported · 1 not installed", viewModel.AvailabilitySummary);
        Assert.Equal(AgentInstallState.NotInstalled, viewModel.DefaultAgentOptions.Single(o => o.Id == "b").InstallState);

        viewModel.DetectCommand.Execute(null);

        Assert.True(viewModel.Cards.Single(c => c.PluginId == "b").IsInstalled);
        Assert.Equal("0.5", viewModel.Cards.Single(c => c.PluginId == "b").Version);
        Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);
        Assert.Equal(AgentInstallState.Installed, viewModel.DefaultAgentOptions.Single(o => o.Id == "b").InstallState);
        Assert.True(viewModel.DefaultAgentOptions.Single(o => o.Id == "b").IsInstalled);
        Assert.True(viewModel.CanDetect);
        probe.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // The Detect pass probes installation and version only: the Load pass's single check for
        // a (b was still not installed then) remains the only availability check of the run.
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public void DetectCommand_WhileProbing_ShowsDetectingThenSettlesEachCard()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateB = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a" ? gateA.Task : gateB.Task);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards.Single(c => c.PluginId == "a");
        var cardB = viewModel.Cards.Single(c => c.PluginId == "b");
        cardB.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.1.0", "0.2.0");

        viewModel.DetectCommand.Execute(null);

        // Mid-pass: every card announces the probe and both header actions are gated.
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.Equal(AgentInstallState.Detecting, cardA.InstallState);
        Assert.Equal(AgentInstallState.Detecting, cardB.InstallState);

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => cardA.InstallState == AgentInstallState.Installed, TimeSpan.FromSeconds(5)));
        Assert.Equal("2.0", cardA.Version);

        gateB.SetResult(new AgentProbeResult(false, null));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CanDetect && viewModel.CanCheckUpdates, TimeSpan.FromSeconds(5)));

        // b settled as not installed: raw null version, stale update indicator cleared, and no
        // availability check ever ran for it.
        Assert.Equal(AgentInstallState.NotInstalled, cardB.InstallState);
        Assert.Null(cardB.Version);
        Assert.False(cardB.HasUpdateAvailable);

        // The Load pass checked each card exactly once; the Detect settle adds no availability
        // check at all — an installed card keeps its shown indicator as it is.
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Once);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public void DetectCardCommand_WhenProbeReportsNotInstalled_ClearsStaleAvailabilityWithoutCheck()
    {
        var a = Launchable("a");
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(false, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "9.9.9");
        Assert.True(card.HasUpdateAvailable);

        viewModel.DetectCardCommand.Execute(card);

        Assert.Equal(AgentInstallState.NotInstalled, card.InstallState);
        Assert.False(card.IsInstalled);
        Assert.Null(card.Version);
        Assert.Null(card.UpdateAvailability);
        Assert.False(card.HasUpdateAvailable);
        Assert.Equal("Not installed on the host", card.Message);

        // The Load pass checked once; the probe itself must not check again.
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public void DetectCardCommand_WhenProbeThrows_RestoresPreviousCardState()
    {
        var a = Launchable("a");
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        var versionBefore = card.Version;
        Assert.Equal(AgentInstallState.Installed, card.InstallState);

        viewModel.DetectCardCommand.Execute(card);

        Assert.Equal(AgentInstallState.Installed, card.InstallState);
        Assert.Equal(versionBefore, card.Version);
        Assert.Null(card.Message);
        Assert.True(viewModel.CanDetect);
    }


    [Fact]
    public void DetectCommand_WhenProbeThrows_RestoresEveryCardToPreviousState()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        probe
            .Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards.Single(c => c.PluginId == "a");
        var cardB = viewModel.Cards.Single(c => c.PluginId == "b");

        viewModel.DetectCommand.Execute(null);

        // No card may stay stuck in Detecting after an abandoned pass.
        Assert.Equal(AgentInstallState.Installed, cardA.InstallState);
        Assert.Equal(AgentInstallState.Installed, cardB.InstallState);
        Assert.Equal("1.0", cardA.Version);
        Assert.Equal("1.0", cardB.Version);
        Assert.True(viewModel.CanDetect);
        probe.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public void DetectCardCommand_WhileHeaderActionRuns_CanExecuteIsFalse()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.DetectCommand.Execute(null);

        // Mid-pass: the per-card Detect command is unavailable alongside both header actions.
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(card));

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => viewModel.DetectCardCommand.CanExecute(card), TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.DetectCommand.CanExecute(null));
        Assert.True(viewModel.CheckUpdatesCommand.CanExecute(null));
    }


    [Fact]
    public void DetectCommand_RaisesCanExecuteChangedAtPassStartAndEnd()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        var detectRaises = 0;
        var checkRaises = 0;
        var cardRaises = 0;
        viewModel.DetectCommand.CanExecuteChanged += (_, _) => detectRaises++;
        viewModel.CheckUpdatesCommand.CanExecuteChanged += (_, _) => checkRaises++;
        viewModel.DetectCardCommand.CanExecuteChanged += (_, _) => cardRaises++;

        viewModel.DetectCommand.Execute(null);

        // The pass start re-queries every gated command, which now reports busy.
        Assert.Equal(1, detectRaises);
        Assert.Equal(1, checkRaises);
        Assert.Equal(1, cardRaises);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(card));

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => cardRaises >= 2, TimeSpan.FromSeconds(5)));

        // The pass end re-queries again (cardRaises is raised last, so the other two are done).
        Assert.Equal(2, detectRaises);
        Assert.Equal(2, checkRaises);
        Assert.Equal(2, cardRaises);
        Assert.True(viewModel.DetectCommand.CanExecute(null));
        Assert.True(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.True(viewModel.DetectCardCommand.CanExecute(card));
    }


    [Fact]
    public void DetectCardCommand_WhilePerCardDetectRuns_DisablesOnlyTheActingCardAndGatesHeader()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards[0];
        var cardB = viewModel.Cards[1];

        viewModel.DetectCardCommand.Execute(cardA);

        // Mid-detect: only the acting card's own actions are disabled — card B stays enabled
        // under its own rules — while the header actions are gated by the running card action.
        Assert.True(cardA.IsDetecting);
        Assert.True(viewModel.IsCardActionRunning);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.False(cardA.CanDetect);
        Assert.False(cardA.CanCheckUpdates);
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));
        Assert.True(cardB.CanDetect);
        Assert.True(cardB.CanCheckUpdates);

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => viewModel.DetectCardCommand.CanExecute(cardA), TimeSpan.FromSeconds(5)));
        Assert.False(cardA.IsDetecting);
        Assert.False(viewModel.IsCardActionRunning);
        Assert.True(viewModel.CanDetect);
        Assert.True(viewModel.CanCheckUpdates);
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));
    }


    [Fact]
    public void DetectCardCommand_WhenProbeThrowsAfterStart_ResetsDetectingFlagAndRunningCounter()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => gate.Task);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.DetectCardCommand.Execute(card);
        Assert.True(card.IsDetecting);
        Assert.True(viewModel.IsCardActionRunning);
        Assert.Equal(AgentInstallState.Detecting, card.InstallState);

        gate.SetException(new InvalidOperationException("probe failed"));
        Assert.True(SpinWait.SpinUntil(
            () => !viewModel.IsCardActionRunning && !card.IsDetecting,
            TimeSpan.FromSeconds(5)));

        // The finally settles every boundary state: the counter, the card's busy flag, the
        // card's install state (restored by the catch), and both per-card commands.
        Assert.False(viewModel.IsCardActionRunning);
        Assert.False(card.IsDetecting);
        Assert.Equal(AgentInstallState.Installed, card.InstallState);
        Assert.True(viewModel.CanDetect);
        Assert.True(viewModel.CanCheckUpdates);
        Assert.True(viewModel.DetectCardCommand.CanExecute(card));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(card));
    }


    [Fact]
    public void DetectCardCommand_WhenDetectCompletes_FinalCanExecuteChangedSeesIdleCard()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards[0];
        var cardB = viewModel.Cards[1];

        // Record what every raise observes: WPF caches the CanExecute result it queries during
        // CanExecuteChanged, so the final raises of the completion boundary must see the acting
        // card already idle — a raise while the card still reports busy would cache false and
        // leave the button disabled until an unrelated re-query.
        bool? actingDetect = null;
        bool? actingCheck = null;
        bool? otherDetect = null;
        bool? otherCheck = null;
        viewModel.DetectCardCommand.CanExecuteChanged += (_, _) => actingDetect = viewModel.DetectCardCommand.CanExecute(cardA);
        viewModel.CheckCardUpdatesCommand.CanExecuteChanged += (_, _) => actingCheck = viewModel.CheckCardUpdatesCommand.CanExecute(cardA);
        viewModel.DetectCardCommand.CanExecuteChanged += (_, _) => otherDetect = viewModel.DetectCardCommand.CanExecute(cardB);
        viewModel.CheckCardUpdatesCommand.CanExecuteChanged += (_, _) => otherCheck = viewModel.CheckCardUpdatesCommand.CanExecute(cardB);

        viewModel.DetectCardCommand.Execute(cardA);

        // The start raises observed the acting card busy; the other card was never gated.
        Assert.True(actingDetect is false);
        Assert.True(actingCheck is false);
        Assert.True(otherDetect is true);
        Assert.True(otherCheck is true);

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        SpinWait.SpinUntil(() => actingDetect == true && actingCheck == true, TimeSpan.FromSeconds(5));

        // The final raises of the completion boundary observed the idle card — on the acting
        // card and the other card alike.
        Assert.True(actingDetect == true);
        Assert.True(actingCheck == true);
        Assert.True(otherDetect == true);
        Assert.True(otherCheck == true);
    }


    [Fact]
    public void DetectCardCommand_RaisesCanExecuteChangedAtDetectStartAndEnd()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProbe(probe).WithUpdate(update).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        var otherCard = viewModel.Cards[1];
        var detectRaises = 0;
        var checkRaises = 0;
        var cardDetectRaises = 0;
        var cardCheckRaises = 0;
        viewModel.DetectCommand.CanExecuteChanged += (_, _) => detectRaises++;
        viewModel.CheckUpdatesCommand.CanExecuteChanged += (_, _) => checkRaises++;
        viewModel.DetectCardCommand.CanExecuteChanged += (_, _) => cardDetectRaises++;
        viewModel.CheckCardUpdatesCommand.CanExecuteChanged += (_, _) => cardCheckRaises++;

        viewModel.DetectCardCommand.Execute(card);

        // Start boundary: one counter enter raises the two header commands, and the explicit
        // per-card raises add exactly one each for the two per-card commands — 1 per command.
        Assert.Equal(1, detectRaises);
        Assert.Equal(1, checkRaises);
        Assert.Equal(1, cardDetectRaises);
        Assert.Equal(1, cardCheckRaises);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(card));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(card));
        // The other card's commands re-query and stay enabled: one card's action never gates
        // another card's commands.
        Assert.True(viewModel.DetectCardCommand.CanExecute(otherCard));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(otherCard));

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => cardCheckRaises >= 2, TimeSpan.FromSeconds(5)));

        // End boundary: one counter exit raises the two header commands and the explicit
        // per-card raises add one more each — 2 per command in total (cardCheckRaises is raised
        // last, so the others are done).
        Assert.Equal(2, detectRaises);
        Assert.Equal(2, checkRaises);
        Assert.Equal(2, cardDetectRaises);
        Assert.Equal(2, cardCheckRaises);
        Assert.True(viewModel.DetectCommand.CanExecute(null));
        Assert.True(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.True(viewModel.DetectCardCommand.CanExecute(card));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(card));
    }

}