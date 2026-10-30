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
    public void CheckUpdatesCommand_WithInstalledCards_InvalidatesThenChecksEach()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var order = new List<string>();
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.InvalidateFor(It.IsAny<Plugin>()))
            .Callback<Plugin>(plugin => order.Add($"invalidate:{plugin.Id}"));
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) =>
            {
                order.Add($"check:{plugin.Id}");
                return Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        order.Clear(); // Discard the Load pass's checks; the forced pass is what is under test.

        viewModel.CheckUpdatesCommand.Execute(null);

        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.InvalidateFor(b), Times.Once);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Load + forced
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Load + forced

        // Invalidation must precede each check so the forced check bypasses the cached TTL result.
        Assert.Equal(new[] { "invalidate:a", "check:a", "invalidate:b", "check:b" }, order);
    }


    [Fact]
    public void CheckUpdatesCommand_WithNotInstalledCard_SkipsCheckAndClearsStaleAvailability()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync("1.0");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(version).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards.Single(c => c.PluginId == "a");
        var cardB = viewModel.Cards.Single(c => c.PluginId == "b");
        cardB.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.1.0", "0.2.0");

        viewModel.CheckUpdatesCommand.Execute(null);

        // The skipped card keeps no stale indicator and is never invalidated or checked.
        Assert.Null(cardB.UpdateAvailability);
        Assert.False(cardB.HasUpdateAvailable);
        availability.Verify(x => x.InvalidateFor(b), Times.Never);
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Never);
        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Load + forced

        // The forced pass is availability-only: no probe and no extra version command at the
        // call site (a was versioned once, by the Load pass; b never at all).
        probe.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Never);
        version.Verify(x => x.GetVersionAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        version.Verify(x => x.GetVersionAsync(b, It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal("1.0", cardA.Version);
    }


    [Fact]
    public void CheckUpdatesCommand_AssignsFreshResultToInstalledCard()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .SetupSequence(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown))) // Load
            .Returns(Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"))); // forced
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        Assert.False(card.HasUpdateAvailable);

        viewModel.CheckUpdatesCommand.Execute(null);

        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("1.0.0 → 2.0.0", card.UpdateAvailabilityText);
    }


    [Theory]
    [InlineData(true, true, AgentUpdateStatus.UpdateAvailable, AgentUpdateStatus.UpdateAvailable, "Checked 2 agents · 2 updates available")]
    [InlineData(true, true, AgentUpdateStatus.UpdateAvailable, AgentUpdateStatus.UpToDate, "Checked 2 agents · 1 update available")]
    [InlineData(true, true, AgentUpdateStatus.UpToDate, AgentUpdateStatus.UpToDate, "Checked 2 agents · no updates")]
    [InlineData(true, false, AgentUpdateStatus.UpdateAvailable, AgentUpdateStatus.UpToDate, "Checked 1 agent · 1 update available")]
    [InlineData(true, false, AgentUpdateStatus.UpToDate, AgentUpdateStatus.UpToDate, "Checked 1 agent · no updates")]
    [InlineData(false, false, AgentUpdateStatus.UpToDate, AgentUpdateStatus.UpToDate, "No installed agents to check")]
    public void CheckUpdatesCommand_WithMixedInstallStates_ReportsExpectedSummary(
        bool aInstalled,
        bool bInstalled,
        AgentUpdateStatus aStatus,
        AgentUpdateStatus bStatus,
        string expected)
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(aInstalled);
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(bInstalled);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) =>
            {
                var status = plugin.Id == "a" ? aStatus : bStatus;
                return Task.FromResult(status == AgentUpdateStatus.UpdateAvailable
                    ? new AgentUpdateAvailability(status, "1.0.0", "2.0.0")
                    : new AgentUpdateAvailability(status));
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);

        Assert.Equal(expected, viewModel.UpdateCheckSummary);
    }


    [Fact]
    public void CheckUpdatesCommand_SecondCheckClearsSummaryAtStart()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                2 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate)), // first check
                _ => gate.Task // second check: hold the pass open after its start-clear
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);
        Assert.True(SpinWait.SpinUntil(() => viewModel.UpdateCheckSummary is not null, TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.CanCheckUpdates);

        // The second pass clears the summary at start and only rebuilds it when every card has
        // settled — while it waits, the line is gone.
        viewModel.CheckUpdatesCommand.Execute(null);
        Assert.Null(viewModel.UpdateCheckSummary);
        Assert.False(viewModel.CanCheckUpdates);

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        Assert.True(SpinWait.SpinUntil(() => viewModel.UpdateCheckSummary is not null, TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.CanCheckUpdates);
    }


    [Fact]
    public void CheckUpdatesCommand_WhileChecking_GatesDetectAndRestoresAfter()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // forced check: hold the pass open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.CheckUpdatesCommand.Execute(null);

        // Mid-pass: both header actions are gated and the card never flips to Detecting.
        Assert.True(viewModel.IsCheckingUpdates);
        Assert.False(viewModel.IsDetecting);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.Equal(AgentInstallState.Installed, card.InstallState);

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CanDetect && viewModel.CanCheckUpdates, TimeSpan.FromSeconds(5)));
        Assert.True(card.HasUpdateAvailable);
    }


    [Fact]
    public void CheckUpdatesCommand_RaisesCanExecuteChangedAtPassStartAndEnd()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // forced check: hold the pass open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        var detectRaises = 0;
        var checkRaises = 0;
        var cardRaises = 0;
        viewModel.DetectCommand.CanExecuteChanged += (_, _) => detectRaises++;
        viewModel.CheckUpdatesCommand.CanExecuteChanged += (_, _) => checkRaises++;
        viewModel.DetectCardCommand.CanExecuteChanged += (_, _) => cardRaises++;

        viewModel.CheckUpdatesCommand.Execute(null);

        // The pass start re-queries every gated command, which now reports busy.
        Assert.Equal(1, detectRaises);
        Assert.Equal(1, checkRaises);
        Assert.Equal(1, cardRaises);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(card));

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
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
    public void CheckCardUpdatesCommand_WithInstalledIdleCard_CanExecuteIsTrue()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        Assert.True(card.IsInstalled);
        Assert.False(card.IsCheckingUpdates);
        Assert.True(card.CanCheckUpdates);
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(card));
    }


    [Theory]
    [InlineData(AgentInstallState.Detecting)]
    [InlineData(AgentInstallState.NotInstalled)]
    public void CheckCardUpdatesCommand_WithNotInstallableCard_CanExecuteIsFalse(AgentInstallState state)
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        card.InstallState = state;

        Assert.False(card.CanCheckUpdates);
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(card));
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhileHeaderDetectRuns_CanExecuteIsFalseOnEveryCard()
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

        viewModel.DetectCommand.Execute(null);

        // Mid-pass: the per-card Check updates command is unavailable on every card alongside
        // both header actions.
        Assert.True(viewModel.IsDetecting);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));

        gateA.SetResult(new AgentProbeResult(true, "2.0"));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CheckCardUpdatesCommand.CanExecute(cardB), TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhileHeaderCheckUpdatesRuns_CanExecuteIsFalseOnEveryCard()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 or 2 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the forced header pass: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards[0];
        var cardB = viewModel.Cards[1];

        viewModel.CheckUpdatesCommand.Execute(null);

        // Mid-pass: the per-card Check updates command is unavailable on every card alongside
        // both header actions.
        Assert.True(viewModel.IsCheckingUpdates);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CheckCardUpdatesCommand.CanExecute(cardB), TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhilePerCardCheckRuns_DisablesOnlyTheActingCardAndGatesHeader()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 or 2 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards[0];
        var cardB = viewModel.Cards[1];

        viewModel.CheckCardUpdatesCommand.Execute(cardA);

        // Mid-check: only the acting card's own actions are disabled — card B stays enabled
        // under its own rules — while the header actions are gated by the running card action.
        Assert.True(viewModel.IsCardActionRunning);
        Assert.True(cardA.IsCheckingUpdates);
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

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CheckCardUpdatesCommand.CanExecute(cardA), TimeSpan.FromSeconds(5)));
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.False(viewModel.IsCardActionRunning);
        Assert.False(cardA.IsCheckingUpdates);
        Assert.True(cardA.CanDetect);
        Assert.True(cardA.CanCheckUpdates);
        Assert.True(viewModel.CanDetect);
        Assert.True(viewModel.CanCheckUpdates);
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhileTwoPerCardChecksRun_HeaderStaysGatedUntilBothEnd()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateB = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCallsA = 0;
        var checkCallsB = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) =>
            {
                if (plugin.Id == "a")
                {
                    return Interlocked.Increment(ref checkCallsA) == 1
                        ? Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)) // Load
                        : gateA.Task; // the per-card check: hold it open
                }

                return Interlocked.Increment(ref checkCallsB) == 1
                    ? Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)) // Load
                    : gateB.Task; // the per-card check: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards[0];
        var cardB = viewModel.Cards[1];

        viewModel.CheckCardUpdatesCommand.Execute(cardA);

        // Card A's running check must not gate card B's commands: card B stays enabled, the
        // header actions are gated.
        Assert.True(viewModel.IsCardActionRunning);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));

        viewModel.CheckCardUpdatesCommand.Execute(cardB);

        // Both checks run at once: each card is blocked only by its own action; the header
        // stays gated until the last one ends.
        Assert.True(viewModel.IsCardActionRunning);
        Assert.True(cardA.IsCheckingUpdates);
        Assert.True(cardB.IsCheckingUpdates);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.False(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));

        gateA.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CheckCardUpdatesCommand.CanExecute(cardA), TimeSpan.FromSeconds(5)));

        // One of two finished: card A's commands restore while card B's stay blocked by its own
        // still-running check, and the header stays gated by the remaining action.
        Assert.True(viewModel.IsCardActionRunning);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.False(cardA.IsCheckingUpdates);
        Assert.True(cardB.IsCheckingUpdates);
        Assert.False(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.False(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));

        gateB.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CanDetect && viewModel.CanCheckUpdates, TimeSpan.FromSeconds(5)));

        Assert.False(viewModel.IsCardActionRunning);
        Assert.False(cardA.IsCheckingUpdates);
        Assert.False(cardB.IsCheckingUpdates);
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardA));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardA));
        Assert.True(viewModel.DetectCardCommand.CanExecute(cardB));
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(cardB));
        Assert.True(viewModel.DetectCommand.CanExecute(null));
        Assert.True(viewModel.CheckUpdatesCommand.CanExecute(null));
    }


    [Fact]
    public void CheckCardUpdatesCommand_WithTwoInstalledAgents_InvalidatesOnlyTheTargetBeforeChecking()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var order = new List<string>();
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync("1.0");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.InvalidateFor(It.IsAny<Plugin>()))
            .Callback<Plugin>(plugin => order.Add($"invalidate:{plugin.Id}"));
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) =>
            {
                order.Add($"check:{plugin.Id}");
                return Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(version).WithProbe(probe).WithUpdate(update).WithAvailability(availability).Build();
        viewModel.Load(null);
        var cardA = viewModel.Cards.Single(c => c.PluginId == "a");
        var cardB = viewModel.Cards.Single(c => c.PluginId == "b");
        var staleB = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.1.0", "0.2.0");
        cardB.UpdateAvailability = staleB;
        order.Clear(); // Discard the Load pass's checks; the per-card check is what is under test.

        viewModel.CheckCardUpdatesCommand.Execute(cardA);

        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.InvalidateFor(b), Times.Never);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Exactly(2)); // Load + per-card
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Once); // Load only

        // Invalidation must precede the check so it bypasses the cached TTL result.
        Assert.Equal(new[] { "invalidate:a", "check:a" }, order);

        // The other agent's cached result is untouched.
        Assert.Equal(staleB, cardB.UpdateAvailability);

        // The per-card action is availability-only: no probe and no version command at the call
        // site (each agent was versioned exactly once, by the Load pass).
        probe.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Never);
        version.Verify(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }


    [Fact]
    public void CheckCardUpdatesCommand_AssignsFreshResultToCard()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .SetupSequence(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown))) // Load
            .Returns(Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"))); // per-card
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        Assert.False(card.HasUpdateAvailable);

        viewModel.CheckCardUpdatesCommand.Execute(card);

        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("1.0.0 → 2.0.0", card.UpdateAvailabilityText);
    }


    [Theory]
    [InlineData(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0", "Update available · 1.0.0 → 2.0.0")]
    [InlineData(AgentUpdateStatus.UpdateAvailable, null, null, "Update available")]
    [InlineData(AgentUpdateStatus.UpToDate, null, null, "Up to date")]
    [InlineData(AgentUpdateStatus.Unknown, null, null, "Update availability unknown")]
    public void CheckCardUpdatesCommand_WhenCheckSettles_WritesExpectedOutcomeMessage(
        AgentUpdateStatus status,
        string? installedVersion,
        string? latestVersion,
        string expected)
    {
        var a = Launchable("a");
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => Task.FromResult(new AgentUpdateAvailability(status, installedVersion, latestVersion)) // per-card
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.CheckCardUpdatesCommand.Execute(card);

        Assert.Equal(expected, card.Message);
    }


    [Fact]
    public void CheckCardUpdatesCommand_DoesNotTouchHeaderUpdateCheckSummary()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);
        var summary = viewModel.UpdateCheckSummary;
        Assert.NotNull(summary);

        viewModel.CheckCardUpdatesCommand.Execute(viewModel.Cards[0]);

        Assert.Equal(summary, viewModel.UpdateCheckSummary);
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhenExecutedOnNotInstalledCard_ClearsIndicatorWithoutRequestingSource()
    {
        var a = Launchable("a");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(detection).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        card.UpdateAvailability = new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "0.1.0", "0.2.0");

        // RelayCommand<T>.Execute does not consult CanExecute — the handler re-applies the gate.
        viewModel.CheckCardUpdatesCommand.Execute(card);

        Assert.Null(card.UpdateAvailability);
        Assert.Equal("Not installed on the host", card.Message);
        availability.Verify(x => x.InvalidateFor(a), Times.Never);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Never);
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhileChecking_GatesHeaderActionsAndRestoresAfter()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.CheckCardUpdatesCommand.Execute(card);

        // Mid-check: both header actions are gated and the card never flips to Detecting.
        Assert.True(viewModel.IsCardActionRunning);
        Assert.False(viewModel.CanDetect);
        Assert.False(viewModel.CanCheckUpdates);
        Assert.False(viewModel.DetectCommand.CanExecute(null));
        Assert.False(viewModel.CheckUpdatesCommand.CanExecute(null));
        Assert.Equal(AgentInstallState.Installed, card.InstallState);

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate));
        Assert.True(SpinWait.SpinUntil(() => viewModel.CanDetect && viewModel.CanCheckUpdates, TimeSpan.FromSeconds(5)));
        Assert.False(viewModel.IsCardActionRunning);
        Assert.False(card.IsCheckingUpdates);
        Assert.Equal("Up to date", card.Message);
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhenCheckThrows_RestoresGatesAfterFailure()
    {
        var a = Launchable("a");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: fail it below
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
        viewModel.Load(null);
        var card = viewModel.Cards[0];
        Assert.True(SpinWait.SpinUntil(() => card.UpdateAvailability is not null, TimeSpan.FromSeconds(5)));
        var beforeFailure = card.UpdateAvailability;

        viewModel.CheckCardUpdatesCommand.Execute(card);
        Assert.True(viewModel.IsCardActionRunning);

        gate.SetException(new InvalidOperationException("source unreachable"));
        Assert.True(SpinWait.SpinUntil(
            () => !viewModel.IsCardActionRunning && viewModel.CanDetect && viewModel.CanCheckUpdates,
            TimeSpan.FromSeconds(5)));
        Assert.False(card.IsCheckingUpdates);
        Assert.True(viewModel.CheckCardUpdatesCommand.CanExecute(card));
        Assert.Equal(beforeFailure, card.UpdateAvailability);
        Assert.Null(card.Message);
    }


    [Fact]
    public void CheckCardUpdatesCommand_WhenCheckCompletes_FinalCanExecuteChangedSeesIdleCard()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 or 2 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
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

        viewModel.CheckCardUpdatesCommand.Execute(cardA);

        // The start raises observed the acting card busy; the other card was never gated.
        Assert.True(actingDetect is false);
        Assert.True(actingCheck is false);
        Assert.True(otherDetect is true);
        Assert.True(otherCheck is true);

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpToDate));
        SpinWait.SpinUntil(() => actingDetect == true && actingCheck == true, TimeSpan.FromSeconds(5));

        // The final raises of the completion boundary observed the idle card — on the acting
        // card and the other card alike.
        Assert.True(actingDetect == true);
        Assert.True(actingCheck == true);
        Assert.True(otherDetect == true);
        Assert.True(otherCheck == true);
    }


    [Fact]
    public void CheckCardUpdatesCommand_RaisesCanExecuteChangedAtCheckStartAndEnd()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gate = new TaskCompletionSource<AgentUpdateAvailability>(TaskCreationOptions.RunContinuationsAsynchronously);
        var checkCalls = 0;
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, _) => Interlocked.Increment(ref checkCalls) switch
            {
                1 or 2 => Task.FromResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown)), // Load
                _ => gate.Task // the per-card check: hold it open
            });
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).WithOperationLifetime(new DeferredOperationLifetime()).Build();
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

        viewModel.CheckCardUpdatesCommand.Execute(card);

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

        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
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