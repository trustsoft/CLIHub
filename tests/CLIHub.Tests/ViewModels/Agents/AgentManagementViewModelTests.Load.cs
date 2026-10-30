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
    public void Load_BuildsCardsCountsAndDefaultOptions()
    {
        var a = Launchable("a", withUpdate: true);
        var b = Launchable("b");
        var detection = Detection();
        detection.Setup(x => x.IsInstalledInSystem(b)).Returns(false);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(Versions()).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([]).WithDetection(Detection()).WithVersions(Versions()).WithInvalidPlugins([invalid]).Build();
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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("valid")]).WithDetection(Detection()).WithVersions(Versions()).WithInvalidPlugins([invalid]).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("valid")]).WithDetection(detection).WithVersions(Versions()).WithInvalidPlugins([invalid]).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([]).WithDetection(Detection()).WithVersions(Versions()).WithPluginCatalog(catalog).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("a")]).WithDetection(Detection()).WithVersions(Versions()).Build();

        viewModel.Load(null);

        Assert.Equal("None", viewModel.DefaultAgentName);
        Assert.False(viewModel.HasDefaultAgent);
        Assert.Equal("No default agent — choose one", viewModel.DefaultAgentDetail);
        Assert.Equal(SummaryCardTone.Muted, viewModel.DefaultAgentValueTone);
    }


    [Fact]
    public void Load_WithDefaultAgent_UsesNeutralValueTone()
    {
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("a")]).WithDetection(Detection()).WithVersions(Versions()).Build();

        viewModel.Load("a");

        Assert.True(viewModel.HasDefaultAgent);
        Assert.Equal(SummaryCardTone.Neutral, viewModel.DefaultAgentValueTone);
    }


    [Fact]
    public void Load_WithEveryAgentInstalled_ReportsAllInstalled()
    {
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("a"), Launchable("b")]).WithDetection(Detection()).WithVersions(Versions()).Build();

        viewModel.Load(null);

        Assert.Equal("2 supported · all installed", viewModel.AvailabilitySummary);
    }


    [Fact]
    public void Load_WithNoPlugins_ReportsNoAgentPlugins()
    {
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([]).WithDetection(Detection()).WithVersions(Versions()).Build();

        viewModel.Load(null);

        Assert.Equal(0, viewModel.InstalledAgentCount);
        Assert.Equal(0, viewModel.SupportedAgentCount);
        Assert.Equal("No agent plugins found", viewModel.AvailabilitySummary);
    }


    [Fact]
    public void Load_PopulatesVersions()
    {
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([Launchable("a")]).WithDetection(Detection()).WithVersions(Versions("3.4.5")).Build();

        viewModel.Load(null);

        Assert.Equal("3.4.5", viewModel.Cards[0].Version);
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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(version).WithOperationLifetime(new DeferredOperationLifetime()).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(version).WithLogger(logger.Object).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([b]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "b");
        Assert.True(card.HasUpdateAvailable);
        Assert.False(card.CanUpdate);
    }


    [Fact]
    public void Load_MatchingAgentProcessRunning_DisablesUpdateAndExplainsBlock()
    {
        var a = Launchable("a", withUpdate: true);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(Processes(new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now))).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(Processes(
                @"C:\Program Files\nodejs\node.exe",
                @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run")).Build();

        viewModel.Load(null);

        var card = viewModel.Cards.Single(c => c.PluginId == "a");
        Assert.False(card.CanUpdate);
        Assert.Equal("Update blocked while running", card.Message);
        Assert.True(card.HasMessage);

        // An unrelated node script names no declared npm package and blocks nothing.
        var b = Launchable("b", withUpdate: true);
        b.UpdateCheck = new PluginUpdateCheck("npm", "@other/package");
        var unrelatedViewModel = new AgentManagementViewModelBuilder().WithPlugins([b]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(Processes(@"C:\Program Files\nodejs\node.exe", @"node.exe server.js")).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(Processes(new AgentProcessInstance(7, @"C:\tools\b.exe", DateTime.Now))).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b, c]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(processInspector).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(processInspector).Build();

        viewModel.Load(null);

        processInspector.Verify(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()), Times.Never);
    }


    [Fact]
    public void Load_ShowsDeclarativeAvailability_BeforeBlockedStateLands()
    {
        var a = Launchable("a", withUpdate: true);
        var gate = new ManualResetEventSlim(false);
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(GatedProcesses(gate, new AgentProcessInstance(1, @"C:\tools\a.exe", DateTime.Now))).WithOperationLifetime(new DeferredOperationLifetime()).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(inspector).WithOperationLifetime(new DeferredOperationLifetime()).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(Detection()).WithVersions(Versions()).WithProcessInspector(inspector).WithProcessMonitorLogger(processMonitorLogger.Object).Build();

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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithLauncher(launcher).Build();
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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(Versions()).Build();

        viewModel.Load(null);

        Assert.Equal(AgentInstallState.Installed, viewModel.DefaultAgentOptions.Single(o => o.Id is null).InstallState);
        Assert.Equal(AgentInstallState.Installed, viewModel.DefaultAgentOptions.Single(o => o.Id == "a").InstallState);
        Assert.Equal(AgentInstallState.NotInstalled, viewModel.DefaultAgentOptions.Single(o => o.Id == "b").InstallState);
        Assert.False(viewModel.DefaultAgentOptions.Single(o => o.Id == "b").IsInstalled);
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
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a, b]).WithDetection(detection).WithVersions(version).WithAvailability(availability).Build();

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
    public void Load_ClearsUpdateCheckSummary()
    {
        var a = Launchable("a");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        availability
            .Setup(x => x.CheckAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentUpdateAvailability(AgentUpdateStatus.Unknown));
        var viewModel = new AgentManagementViewModelBuilder().WithPlugins([a]).WithDetection(Detection()).WithVersions(Versions()).WithAvailability(availability).Build();
        viewModel.Load(null);

        viewModel.CheckUpdatesCommand.Execute(null);
        Assert.NotNull(viewModel.UpdateCheckSummary);

        viewModel.Load(null);
        Assert.Null(viewModel.UpdateCheckSummary);
    }

}