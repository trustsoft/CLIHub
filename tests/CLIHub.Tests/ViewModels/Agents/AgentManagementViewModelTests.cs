namespace CLIHub.Tests.ViewModels;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Controls;
using CLIHub.Core.Agents;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.ViewModels;
using CLIHub.Lifetime;
using CLIHub.Launching;

public class AgentManagementViewModelTests
{
    private static Plugin Launchable(string id, bool withUpdate = false)
    {
        var plugin = new Plugin
        {
            Id = id,
            Name = id,
            PluginDirectory = $"C:\\plugins\\{id}",
            Commands = new AgentCommands { Launch = new PluginCommand { Executable = id } }
        };

        if (withUpdate)
        {
            plugin.Commands.Update = new PluginCommand { Executable = id, Arguments = "update" };
        }

        return plugin;
    }

    private static AgentManagementViewModel CreateViewModel(
        Plugin[] plugins,
        Mock<IAgentDetectionService> detection,
        Mock<IAgentVersionService> version,
        Mock<IAgentManagementService> management,
        Mock<IExternalLauncher> launcher,
        Mock<IAgentProcessInspector>? processInspector = null,
        Mock<IAgentUpdateAvailabilityService>? availability = null,
        IApplicationOperationLifetime? operationLifetime = null,
        ILogger<AgentManagementViewModel>? logger = null,
        ILogger<AgentProcessMonitor>? processMonitorLogger = null)
    {
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        if (processInspector is null)
        {
            processInspector = new Mock<IAgentProcessInspector>();
            processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        }

        var processMonitor = new AgentProcessMonitor(
            processInspector.Object,
            new AgentProcessMatcher(),
            catalog.Object,
            processMonitorLogger ?? NullLogger<AgentProcessMonitor>.Instance);

        return new AgentManagementViewModel(
            catalog.Object,
            detection.Object,
            version.Object,
            (availability ?? new Mock<IAgentUpdateAvailabilityService>()).Object,
            management.Object,
            launcher.Object,
            processMonitor,
            operationLifetime ?? new InlineOperationLifetime(),
            logger ?? NullLogger<AgentManagementViewModel>.Instance);
    }

    private static Mock<IAgentProcessInspector> Processes(params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns(processes);
        return inspector;
    }

    private static Mock<IAgentProcessInspector> Processes(string executablePath, string? commandLine = null)
    {
        return Processes(new AgentProcessInstance(1, executablePath, DateTime.Now, commandLine));
    }

    private static Mock<IAgentProcessInspector> GatedProcesses(
        ManualResetEventSlim gate,
        params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns<IReadOnlySet<string>, AgentProcessMatchDetail>((_, _) =>
            {
                gate.Wait();
                return processes;
            });
        return inspector;
    }

    private static Mock<IAgentDetectionService> Detection()
    {
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(true);
        return detection;
    }

    private static Mock<IAgentVersionService> Versions(string value = "1.0")
    {
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync(value);
        return version;
    }

    [Fact]
    public void Load_BuildsCardsCountsAndDefaultOptions()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load("b");

        Assert.Equal(2, viewModel.Cards.Count);
        Assert.Equal(1, viewModel.InstalledAgentCount);
        Assert.Equal(2, viewModel.SupportedAgentCount);
        Assert.Equal("2 supported · 1 not installed", viewModel.AvailabilitySummary);
        Assert.Equal("b", viewModel.CurrentDefaultAgentId);
        Assert.Equal("b", viewModel.DefaultAgentName);
        Assert.True(viewModel.HasDefaultAgent);

        // The default agent is not installed, so it gets no version probe and its detail falls
        // back to the preselection text instead of a version.
        Assert.Equal("Preselected in the launch window", viewModel.DefaultAgentDetail);
        Assert.Contains(viewModel.DefaultAgentOptions, option => option.Id is null);
        Assert.True(viewModel.Cards.Single(c => c.PluginId == "a").CanUpdate);
        Assert.False(viewModel.Cards.Single(c => c.PluginId == "b").CanUpdate);
    }

    [Fact]
    public void Load_WithNoDefaultAgent_ShowsNone()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("None", viewModel.DefaultAgentName);
        Assert.False(viewModel.HasDefaultAgent);
        Assert.Equal("No default agent — choose one", viewModel.DefaultAgentDetail);
        Assert.Equal(SummaryCardTone.Muted, viewModel.DefaultAgentValueTone);
    }

    [Fact]
    public void Load_WithDefaultAgent_UsesNeutralValueTone()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load("a");

        Assert.True(viewModel.HasDefaultAgent);
        Assert.Equal(SummaryCardTone.Neutral, viewModel.DefaultAgentValueTone);
    }

    [Fact]
    public void Load_WithEveryAgentInstalled_ReportsAllInstalled()
    {
        var viewModel = CreateViewModel([Launchable("a"), Launchable("b")], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);
    }

    [Fact]
    public void Load_WithNoPlugins_ReportsNoAgentPlugins()
    {
        var viewModel = CreateViewModel([], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal(0, viewModel.InstalledAgentCount);
        Assert.Equal(0, viewModel.SupportedAgentCount);
        Assert.Equal("No agent plugins found", viewModel.AvailabilitySummary);
    }

    [Fact]
    public void Load_PopulatesVersions()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions("3.4.5"), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("3.4.5", viewModel.Cards[0].Version);
    }

    [Fact]
    public void Dispose_CancelsInFlightPopulation()
    {
        var a = Launchable("a");
        var versionCompletion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var capturedToken = CancellationToken.None;
        var version = new Mock<IAgentVersionService>();
        version
            .Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((_, token) =>
            {
                capturedToken = token;
                return versionCompletion.Task;
            });
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            version,
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
        viewModel.Load(null);

        viewModel.Dispose();
        versionCompletion.SetResult("1.0");

        Assert.True(capturedToken.IsCancellationRequested);
        Assert.Null(viewModel.Cards[0].Version);
    }

    [Fact]
    public void Load_SecondLoadCancelsFirstPass()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var firstPassCompletion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstPassBlocked = true;
        var firstPassToken = CancellationToken.None;
        var version = new Mock<IAgentVersionService>();
        version
            .Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, token) =>
            {
                if (plugin.Id == "a" && firstPassBlocked)
                {
                    firstPassBlocked = false;
                    firstPassToken = token;
                    return firstPassCompletion.Task;
                }

                return Task.FromResult<string?>("2.0");
            });
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            version,
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());

        viewModel.Load(null);
        viewModel.Load(null);
        firstPassCompletion.SetResult("9.9");

        Assert.True(firstPassToken.IsCancellationRequested);
        Assert.Equal("2.0", viewModel.Cards.Single(card => card.PluginId == "a").Version);
        Assert.Equal("2.0", viewModel.Cards.Single(card => card.PluginId == "b").Version);
    }

    [Fact]
    public void PopulateVersions_WhenProbeThrows_LogsErrorAndKeepsOtherCards()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var version = new Mock<IAgentVersionService>();
        version
            .Setup(x => x.GetVersionAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        version
            .Setup(x => x.GetVersionAsync(b, It.IsAny<CancellationToken>()))
            .ReturnsAsync("2.0");
        var logger = new Mock<ILogger<AgentManagementViewModel>>();
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            version,
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            logger: logger.Object);

        viewModel.Load(null);

        logger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        Assert.Null(viewModel.Cards.Single(card => card.PluginId == "a").Version);
        Assert.Equal("2.0", viewModel.Cards.Single(card => card.PluginId == "b").Version);
    }

    [Fact]
    public void Load_PopulatesUpdateAvailabilityPerCard()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "2.0.32", "2.0.33"));
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(card.HasUpdateAvailable);
        Assert.Equal("2.0.32 → 2.0.33", card.UpdateAvailabilityText);
    }

    [Fact]
    public void Load_UnknownAvailability_LeavesCanUpdateUnchanged()
    {
        var a = Launchable("a", withUpdate: true);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.True(card.CanUpdate);
    }

    [Fact]
    public void Load_UpdateAvailableWithoutUpdateCommand_LeavesCanUpdateDisabled()
    {
        var b = Launchable("b", withUpdate: false);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "9.9.9"));
        var viewModel = CreateViewModel([b], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "b");
        Assert.True(card.HasUpdateAvailable);
        Assert.False(card.CanUpdate);
    }

    [Fact]
    public void UpdateCommand_WhenStarted_InvalidatesCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }

    [Fact]
    public void UpdateCommand_WhenNotStarted_KeepsCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Failed, "boom"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(It.IsAny<Plugin>()), Times.Never);
    }

    [Fact]
    public void DetectCardCommand_UpdatesInstallationAndVersionWithoutAvailabilityCheck()
    {
        var a = Launchable("a");
        var management = new Mock<IAgentManagementService>();
        management.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "9.9"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>(), availability: availability);
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
    public void UpdateCommand_ReportsOutcome()
    {
        var a = Launchable("a", withUpdate: true);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update started.", viewModel.Cards[0].Message);
    }

    [Fact]
    public void UpdateCommand_WhenServiceBlocks_SurfacesServiceMessage()
    {
        var a = Launchable("a", withUpdate: true);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Blocked, "Update blocked while the agent is running: a"));
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update blocked while the agent is running: a", viewModel.Cards[0].Message);
    }

    [Fact]
    public void Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock()
    {
        var a = Launchable("a", withUpdate: true);
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            Processes(new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now)));

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while the agent is running: a", card.Message);
        Assert.True(card.HasMessage);
    }

    [Fact]
    public void Load_NodeShimAgentRunning_DisablesUpdateAndExplainsBlock()
    {
        var a = Launchable("a", withUpdate: true);
        a.UpdateCheck = new PluginUpdateCheck("npm", "@opencode/cli");
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            Processes(
                @"C:\Program Files\nodejs\node.exe",
                @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run"));

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while the agent is running: a", card.Message);
        Assert.True(card.HasMessage);

        // An unrelated node script names no declared npm package and blocks nothing.
        var b = Launchable("b", withUpdate: true);
        b.UpdateCheck = new PluginUpdateCheck("npm", "@other/package");
        var unrelatedViewModel = CreateViewModel(
            [b],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            Processes(@"C:\Program Files\nodejs\node.exe", @"node.exe server.js"));

        unrelatedViewModel.Load(null);

        var unrelatedCard = unrelatedViewModel.Cards.Single(c => c.PluginId == "b");
        Assert.True(unrelatedCard.CanUpdate);
        Assert.Null(unrelatedCard.Message);
    }

    [Fact]
    public void Load_OtherAgentsProcessRunning_KeepsUpdateAvailableWithoutMessage()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b", withUpdate: true);
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            Processes(new AgentProcessInstance(7, @"C:\tools\b.exe", DateTime.Now)));

        viewModel.Load(null);

        var cardA = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(cardA.CanUpdate);
        Assert.Null(cardA.Message);
        Assert.False(viewModel.Cards.Single(c => c.PluginId == "b").CanUpdate);
        Assert.NotNull(viewModel.Cards.Single(c => c.PluginId == "b").Message);
    }

    [Fact]
    public void Load_WithNoRunningProcesses_KeepsDeclaredUpdateAvailableWithoutMessage()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b");
        var viewModel = CreateViewModel([a, b], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.True(viewModel.Cards.Single(c => c.PluginId == "a").CanUpdate);
        Assert.False(viewModel.Cards.Single(c => c.PluginId == "b").CanUpdate);
        Assert.All(viewModel.Cards, card => Assert.Null(card.Message));
    }

    [Fact]
    public void Load_MultipleUpdateDeclaringPlugins_EnumeratesProcessesOnce()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b", withUpdate: true);
        var c = Launchable("c", withUpdate: true);
        var processInspector = new Mock<IAgentProcessInspector>();
        processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        var viewModel = CreateViewModel(
            [a, b, c],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            processInspector);

        viewModel.Load(null);

        processInspector.Verify(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()), Times.Once);
    }

    [Fact]
    public void Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var processInspector = new Mock<IAgentProcessInspector>();
        processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            processInspector);

        viewModel.Load(null);

        processInspector.Verify(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()), Times.Never);
    }

    [Fact]
    public void Load_ShowsDeclarativeAvailability_BeforeBlockedStateLands()
    {
        var a = Launchable("a", withUpdate: true);
        var gate = new ManualResetEventSlim(false);
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            GatedProcesses(gate, new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now)),
            operationLifetime: new DeferredOperationLifetime());

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.True(card.CanUpdate);
        Assert.Null(card.Message);

        gate.Set();
        SpinWait.SpinUntil(() => !card.CanUpdate, TimeSpan.FromSeconds(5));

        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while the agent is running: a", card.Message);
    }

    [Fact]
    public void Dispose_CancelsBlockedStatePass()
    {
        var a = Launchable("a", withUpdate: true);
        var gate = new ManualResetEventSlim(false);
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            GatedProcesses(gate, new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now)),
            operationLifetime: new DeferredOperationLifetime());

        viewModel.Load(null);
        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        viewModel.Dispose();
        gate.Set();

        // Give a broken guard time to manifest: after Dispose the pass's token and generation
        // are stale, so its blocking write must never reach the card.
        SpinWait.SpinUntil(() => !card.CanUpdate, TimeSpan.FromMilliseconds(250));

        Assert.True(card.CanUpdate);
        Assert.Null(card.Message);
    }

    [Fact]
    public void Load_SecondLoadCancelsBlockedStatePass()
    {
        var a = Launchable("a", withUpdate: true);
        var gate = new ManualResetEventSlim(false);
        var firstCallStarted = new ManualResetEventSlim(false);
        var enumerationCalls = 0;
        var inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns<IReadOnlySet<string>, AgentProcessMatchDetail>((_, _) =>
            {
                if (Interlocked.Increment(ref enumerationCalls) == 1)
                {
                    firstCallStarted.Set();
                    gate.Wait();
                    return new AgentProcessInstance[] { new(1, @"C:\tools\a.exe", DateTime.Now) };
                }

                return [];
            });
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            inspector,
            operationLifetime: new DeferredOperationLifetime());

        viewModel.Load(null);
        var firstLoadCard = viewModel.Cards.Single(c => c.PluginId == "a");

        // Pin the gated enumeration to the first pass before the second load starts its own.
        Assert.True(firstCallStarted.Wait(TimeSpan.FromSeconds(5)));

        viewModel.Load(null);
        var secondLoadCard = viewModel.Cards.Single(c => c.PluginId == "a");

        gate.Set();
        SpinWait.SpinUntil(() => !firstLoadCard.CanUpdate, TimeSpan.FromMilliseconds(250));

        // The superseded pass's blocking write is rejected by the generation guard; the new
        // load's own pass settles unblocked (its enumeration returned no processes).
        Assert.True(firstLoadCard.CanUpdate);
        Assert.True(secondLoadCard.CanUpdate);
        Assert.Null(secondLoadCard.Message);
    }

    [Fact]
    public void Load_WhenEnumerationFails_KeepsDeclaredAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b", withUpdate: true);
        var inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Throws(new InvalidOperationException("enumeration failed"));
        var processMonitorLogger = new Mock<ILogger<AgentProcessMonitor>>();
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            inspector,
            processMonitorLogger: processMonitorLogger.Object);

        viewModel.Load(null);

        Assert.All(viewModel.Cards, card => Assert.True(card.CanUpdate));
        Assert.All(viewModel.Cards, card => Assert.Null(card.Message));
        processMonitorLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void EditConfigCommand_OpensConfigPath()
    {
        var a = Launchable("a");
        var launcher = new Mock<IExternalLauncher>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), launcher);
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.EditConfigCommand.Execute(card);

        Assert.NotNull(card.ConfigPath);
        launcher.Verify(x => x.Open(card.ConfigPath!), Times.Once);
    }

    [Fact]
    public void Load_MarksUninstalledAgentsDisabledInDefaultOptions()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal(AgentInstallState.Installed, viewModel.DefaultAgentOptions.Single(o => o.Id is null).InstallState);
        Assert.Equal(AgentInstallState.Installed, viewModel.DefaultAgentOptions.Single(o => o.Id == "a").InstallState);
        Assert.Equal(AgentInstallState.NotInstalled, viewModel.DefaultAgentOptions.Single(o => o.Id == "b").InstallState);
        Assert.False(viewModel.DefaultAgentOptions.Single(o => o.Id == "b").IsInstalled);
    }

    [Fact]
    public void DetectCommand_UpdatesCardsSummaryAndOptionAvailability()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var management = new Mock<IAgentManagementService>();
        management.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "2.0"));
        management.Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a, b], detection, Versions(), management, new Mock<IExternalLauncher>(), availability: availability);
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
        management.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Exactly(2));

        // The Detect pass probes installation and version only: the Load pass's single check for
        // a (b was still not installed then) remains the only availability check of the run.
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void Load_WhenCardNotInstalled_SkipsVersionAndAvailabilityCalls()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync("1.0");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a, b], detection, version, new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);

        viewModel.Load(null);

        var cardB = viewModel.Cards.Single(c => c.PluginId == "b");
        Assert.Equal(AgentInstallState.NotInstalled, cardB.InstallState);
        Assert.Null(cardB.Version);
        Assert.Null(cardB.UpdateAvailability);

        // The not-installed card is gated: no version probe, no availability check.
        version.Verify(x => x.GetVersionAsync(b, It.IsAny<CancellationToken>()), Times.Never);
        availability.Verify(x => x.CheckAsync(b, It.IsAny<CancellationToken>()), Times.Never);
        version.Verify(x => x.GetVersionAsync(a, It.IsAny<CancellationToken>()), Times.Once);
        availability.Verify(x => x.CheckAsync(a, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void DetectCommand_WhileProbing_ShowsDetectingThenSettlesEachCard()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var gateB = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a" ? gateA.Task : gateB.Task);
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            management,
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(false, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), management, new Mock<IExternalLauncher>(), availability: availability);
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
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            management,
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        management
            .Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            management,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        management.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Once);
    }

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
        var viewModel = CreateViewModel([a, b], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var management = new Mock<IAgentManagementService>();
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>())).ReturnsAsync("1.0");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a, b], detection, version, management, new Mock<IExternalLauncher>(), availability: availability);
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
        management.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Never);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);

        Assert.Equal(expected, viewModel.UpdateCheckSummary);
    }

    [Fact]
    public void Load_ClearsUpdateCheckSummary()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentManagementService>(), new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);
        Assert.NotNull(viewModel.UpdateCheckSummary);

        viewModel.Load(null);
        Assert.Null(viewModel.UpdateCheckSummary);
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
    public void DetectCardCommand_WhileHeaderActionRuns_CanExecuteIsFalse()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var gateA = new TaskCompletionSource<AgentProbeResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            management,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var management = new Mock<IAgentManagementService>();
        management
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            management,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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

    private sealed class InlineOperationLifetime : IApplicationOperationLifetime
    {
        public CancellationToken Token => CancellationToken.None;

        public Task RunAsync(string operationName, Func<CancellationToken, Task> operation)
        {
            var task = operation(CancellationToken.None);
            task.GetAwaiter().GetResult();
            return task;
        }

        public Task StopAsync(TimeSpan timeout) => Task.CompletedTask;
    }

    private sealed class DeferredOperationLifetime : IApplicationOperationLifetime
    {
        public CancellationToken Token => CancellationToken.None;

        public Task RunAsync(string operationName, Func<CancellationToken, Task> operation) =>
            operation(CancellationToken.None);

        public Task StopAsync(TimeSpan timeout) => Task.CompletedTask;
    }
}
