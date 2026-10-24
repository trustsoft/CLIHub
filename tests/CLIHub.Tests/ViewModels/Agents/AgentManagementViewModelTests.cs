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
            processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>())).Returns([]);
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
        inspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>())).Returns(processes);
        return inspector;
    }

    private static Mock<IAgentProcessInspector> GatedProcesses(
        ManualResetEventSlim gate,
        params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()))
            .Returns<IReadOnlySet<string>>(_ =>
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
        Assert.Equal("Version 1.0", viewModel.DefaultAgentDetail);
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
        Assert.Equal("…", viewModel.Cards[0].Version);
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
        Assert.Equal("…", viewModel.Cards.Single(card => card.PluginId == "a").Version);
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
    public void TestCommand_ProbeUpdatesCardAndAvailability()
    {
        var a = Launchable("a");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(a)).Returns(false);
        var management = new Mock<IAgentManagementService>();
        management.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "9.9"));
        var viewModel = CreateViewModel([a], detection, Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);
        Assert.Equal("1 supported · 1 not installed", viewModel.AvailabilitySummary);

        viewModel.TestCommand.Execute(viewModel.Cards[0]);

        Assert.True(viewModel.Cards[0].IsInstalled);
        Assert.Equal("9.9", viewModel.Cards[0].Version);
        Assert.Equal("1 supported · all installed", viewModel.AvailabilitySummary);
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
        processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>())).Returns([]);
        var viewModel = CreateViewModel(
            [a, b, c],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            processInspector);

        viewModel.Load(null);

        processInspector.Verify(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()), Times.Once);
    }

    [Fact]
    public void Load_NoPluginDeclaresUpdate_DoesNotEnumerateProcesses()
    {
        var a = Launchable("a");
        var b = Launchable("b");
        var processInspector = new Mock<IAgentProcessInspector>();
        processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>())).Returns([]);
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentManagementService>(),
            new Mock<IExternalLauncher>(),
            processInspector);

        viewModel.Load(null);

        processInspector.Verify(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()), Times.Never);
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
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()))
            .Returns<IReadOnlySet<string>>(_ =>
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
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>()))
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

        Assert.True(viewModel.DefaultAgentOptions.Single(o => o.Id is null).IsInstalled);
        Assert.True(viewModel.DefaultAgentOptions.Single(o => o.Id == "a").IsInstalled);
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
        var viewModel = CreateViewModel([a, b], detection, Versions(), management, new Mock<IExternalLauncher>());
        viewModel.Load(null);
        Assert.Equal("2 supported · 1 not installed", viewModel.AvailabilitySummary);
        Assert.False(viewModel.DefaultAgentOptions.Single(o => o.Id == "b").IsInstalled);

        viewModel.DetectCommand.Execute(null);

        Assert.True(viewModel.Cards.Single(c => c.PluginId == "b").IsInstalled);
        Assert.Equal("0.5", viewModel.Cards.Single(c => c.PluginId == "b").Version);
        Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);
        Assert.True(viewModel.DefaultAgentOptions.Single(o => o.Id == "b").IsInstalled);
        Assert.True(viewModel.CanDetect);
        management.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
