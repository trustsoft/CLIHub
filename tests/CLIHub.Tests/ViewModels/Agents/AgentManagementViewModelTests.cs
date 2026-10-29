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
        Mock<IAgentProbeService> probe,
        Mock<IAgentUpdateService> update,
        Mock<IExternalLauncher> launcher,
        Mock<IAgentProcessInspector>? processInspector = null,
        Mock<IAgentUpdateAvailabilityService>? availability = null,
        IApplicationOperationLifetime? operationLifetime = null,
        ILogger<AgentManagementViewModel>? logger = null,
        ILogger<AgentProcessMonitor>? processMonitorLogger = null,
        IReadOnlyList<InvalidPluginEntry>? invalidPlugins = null,
        Mock<IPluginCatalog>? pluginCatalog = null)
    {
        var catalog = pluginCatalog ?? new Mock<IPluginCatalog>();
        if (pluginCatalog is null)
        {
            catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);
        }
        if (pluginCatalog is null || invalidPlugins is not null)
        {
            catalog.Setup(x => x.GetInvalidPlugins()).Returns(invalidPlugins ?? []);
        }

        if (processInspector is null)
        {
            processInspector = new Mock<IAgentProcessInspector>();
            processInspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns([]);
        }

        var processMonitor = new AgentProcessMonitor(
            new ProcessOrchestrator(
                Mock.Of<IRuntimeResolver>(),
                Mock.Of<ICommandLineBuilder>(),
                Mock.Of<IInteractiveProcessRunner>(),
                Mock.Of<IProcessOutputRunner>(),
                new AgentProcessFinder(processInspector.Object, new AgentProcessMatcher()),
                new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
                NullLogger<ProcessOrchestrator>.Instance),
            catalog.Object,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            processMonitorLogger ?? NullLogger<AgentProcessMonitor>.Instance);

        return new AgentManagementViewModel(
            catalog.Object,
            detection.Object,
            version.Object,
            (availability ?? new Mock<IAgentUpdateAvailabilityService>()).Object,
            probe.Object,
            update.Object,
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
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

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
    public void Load_WithInvalidPlugins_PopulatesInvalidCards()
    {
        var invalid = new InvalidPluginEntry("C:\\plugins\\broken", null, null,
            new PluginValidationResult(["Missing id", "Bad command"]));
        var viewModel = CreateViewModel([], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), invalidPlugins: [invalid]);
        viewModel.Load(null);
        Assert.Single(viewModel.InvalidCards);
        Assert.Equal("Missing id; Bad command", viewModel.InvalidCards[0].Reason);
        Assert.False(viewModel.InvalidCards[0].IsEnabled);
    }

    [Fact]
    public void Load_WithValidAndInvalidPlugins_SeparatesCardSets()
    {
        var invalid = new InvalidPluginEntry("C:\\plugins\\broken", null, null,
            new PluginValidationResult(["Missing id"]));
        var viewModel = CreateViewModel([Launchable("valid")], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), invalidPlugins: [invalid]);

        viewModel.Load(null);

        Assert.Single(viewModel.Cards);
        Assert.Equal("valid", viewModel.Cards[0].PluginId);
        Assert.Single(viewModel.InvalidCards);
        Assert.Equal("broken", viewModel.InvalidCards[0].Name);
    }

    [Fact]
    public void Load_WithInvalidPlugins_ValidCountsUnchanged()
    {
        var invalid = new InvalidPluginEntry("C:\\plugins\\broken", null, null,
            new PluginValidationResult(["Missing id"]));
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(It.IsAny<Plugin>())).Returns(false);
        var viewModel = CreateViewModel([Launchable("valid")], detection, Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), invalidPlugins: [invalid]);

        viewModel.Load(null);

        Assert.Single(viewModel.InvalidCards);
        Assert.Equal(1, viewModel.SupportedAgentCount);
        Assert.Equal(0, viewModel.InstalledAgentCount);
    }

    [Fact]
    public void Load_AfterReload_RefreshesBothSets()
    {
        var firstInvalid = new InvalidPluginEntry("C:\\plugins\\first", null, null,
            new PluginValidationResult(["First error"]));
        var secondInvalid = new InvalidPluginEntry("C:\\plugins\\second", null, null,
            new PluginValidationResult(["Second error"]));
        IReadOnlyList<InvalidPluginEntry> currentInvalid = [firstInvalid];
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(() => [Launchable("valid")]);
        catalog.Setup(x => x.GetInvalidPlugins()).Returns(() => currentInvalid);
        var viewModel = CreateViewModel([], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), pluginCatalog: catalog);

        viewModel.Load(null);
        Assert.Equal("First error", Assert.Single(viewModel.InvalidCards).Reason);
        currentInvalid = [secondInvalid];

        catalog.Raise(x => x.PluginsChanged += null, EventArgs.Empty);

        Assert.Equal("Second error", Assert.Single(viewModel.InvalidCards).Reason);
        Assert.Single(viewModel.Cards);
    }

    [Fact]
    public void Load_WithNoDefaultAgent_ShowsNone()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("None", viewModel.DefaultAgentName);
        Assert.False(viewModel.HasDefaultAgent);
        Assert.Equal("No default agent — choose one", viewModel.DefaultAgentDetail);
        Assert.Equal(SummaryCardTone.Muted, viewModel.DefaultAgentValueTone);
    }

    [Fact]
    public void Load_WithDefaultAgent_UsesNeutralValueTone()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

        viewModel.Load("a");

        Assert.True(viewModel.HasDefaultAgent);
        Assert.Equal(SummaryCardTone.Neutral, viewModel.DefaultAgentValueTone);
    }

    [Fact]
    public void Load_WithEveryAgentInstalled_ReportsAllInstalled()
    {
        var viewModel = CreateViewModel([Launchable("a"), Launchable("b")], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);
    }

    [Fact]
    public void Load_WithNoPlugins_ReportsNoAgentPlugins()
    {
        var viewModel = CreateViewModel([], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

        viewModel.Load(null);

        Assert.Equal(0, viewModel.InstalledAgentCount);
        Assert.Equal(0, viewModel.SupportedAgentCount);
        Assert.Equal("No agent plugins found", viewModel.AvailabilitySummary);
    }

    [Fact]
    public void Load_PopulatesVersions()
    {
        var viewModel = CreateViewModel([Launchable("a")], Detection(), Versions("3.4.5"), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);

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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);

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
        var viewModel = CreateViewModel([b], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "b");
        Assert.True(card.HasUpdateAvailable);
        Assert.False(card.CanUpdate);
    }

    [Fact]
    public void UpdateCommand_WhenStarted_InvalidatesCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(a), Times.Once);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }

    [Fact]
    public void UpdateCommand_WhenNotStarted_KeepsCachedAvailability()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Failed, "boom"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>(), availability: availability);
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        availability.Verify(x => x.InvalidateFor(It.IsAny<Plugin>()), Times.Never);
    }

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
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>(), availability: availability);
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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Started, null));
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>());
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update started.", viewModel.Cards[0].Message);
    }

    [Fact]
    public void UpdateCommand_WhenServiceBlocks_SurfacesServiceMessage()
    {
        var a = Launchable("a", withUpdate: true);
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        update
            .Setup(x => x.UpdateAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateResult(AgentUpdateOutcome.Blocked, "Update blocked while running"));
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>());
        viewModel.Load(null);

        viewModel.UpdateCommand.Execute(viewModel.Cards[0]);

        Assert.Equal("Update blocked while running", viewModel.Cards[0].Message);
    }

    [Fact]
    public void Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock()
    {
        var a = Launchable("a", withUpdate: true);
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            Processes(new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now)));

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while running", card.Message);
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            Processes(
                @"C:\Program Files\nodejs\node.exe",
                @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run"));

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while running", card.Message);
        Assert.True(card.HasMessage);

        // An unrelated node script names no declared npm package and blocks nothing.
        var b = Launchable("b", withUpdate: true);
        b.UpdateCheck = new PluginUpdateCheck("npm", "@other/package");
        var unrelatedViewModel = CreateViewModel(
            [b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
        var viewModel = CreateViewModel([a, b], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
        Assert.Equal("Update blocked while running", card.Message);
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), launcher);
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
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>());

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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe.Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "2.0"));
        probe.Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>())).ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a, b], detection, Versions(), probe, update, new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a, b], detection, version, new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);

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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
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
            probe,
            update,
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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(false, null));
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(a, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), probe, update, new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            probe,
            update,
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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(a, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("probe failed"));
        probe
            .Setup(x => x.ProbeAsync(b, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentProbeResult(true, "0.5"));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
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
        probe.Verify(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var viewModel = CreateViewModel([a, b], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a, b], detection, version, probe, update, new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a, b], detection, Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
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
        var probe = new Mock<IAgentProbeService>();
        var update = new Mock<IAgentUpdateService>();
        probe
            .Setup(x => x.ProbeAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .Returns<Plugin, CancellationToken>((plugin, _) => plugin.Id == "a"
                ? gateA.Task
                : Task.FromResult(new AgentProbeResult(true, "1.0")));
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
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
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
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

    [Fact]
    public void CheckCardUpdatesCommand_WithInstalledIdleCard_CanExecuteIsTrue()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            probe,
            update,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel([a, b], Detection(), version, probe, update, new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], Detection(), Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel([a], detection, Versions(), new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(), new Mock<IExternalLauncher>(), availability: availability);
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
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
        var viewModel = CreateViewModel(
            [a, b],
            Detection(),
            Versions(),
            probe,
            update,
            new Mock<IExternalLauncher>(),
            operationLifetime: new DeferredOperationLifetime());
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

    [Fact]
    public void Dispose_WhilePerCardCheckHeldOpen_DiscardsWritesAndResetsFlags()
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
                _ => gate.Task // the per-card check: complete it after Dispose
            });
        var viewModel = CreateViewModel(
            [a],
            Detection(),
            Versions(),
            new Mock<IAgentProbeService>(), new Mock<IAgentUpdateService>(),
            new Mock<IExternalLauncher>(),
            availability: availability,
            operationLifetime: new DeferredOperationLifetime());
        viewModel.Load(null);
        var card = viewModel.Cards[0];

        viewModel.CheckCardUpdatesCommand.Execute(card);
        Assert.True(viewModel.IsCardActionRunning);

        viewModel.Dispose();
        gate.SetResult(new AgentUpdateAvailability(AgentUpdateStatus.UpdateAvailable, "1.0.0", "2.0.0"));

        Assert.True(SpinWait.SpinUntil(
            () => !viewModel.IsCardActionRunning && !card.IsCheckingUpdates,
            TimeSpan.FromSeconds(5)));

        // The late result and message writes are discarded; both busy flags still reset.
        Assert.False(card.HasUpdateAvailable);
        Assert.Null(card.UpdateAvailabilityText);
        Assert.Null(card.Message);
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
