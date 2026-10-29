namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Processes;
using CLIHub.Core.Tests.Fakes;

using ProcessResult = CLIHub.Core.Processes.ProcessResult;

public class AgentManagementServiceTests
{
    private static Plugin AgentPlugin(string id = "opencode", bool withUpdate = true) => new()
    {
        Id = id,
        Name = id,
        Commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = id },
            Version = new PluginCommand { Executable = id, Arguments = "--version" },
            Update = withUpdate ? new PluginCommand { Executable = id, Arguments = "update" } : null
        }
    };

    private static AgentProcessMonitor Monitor(Plugin[] plugins, Mock<IAgentProcessInspector>? inspector = null)
    {
        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        inspector ??= new Mock<IAgentProcessInspector>();

        return new AgentProcessMonitor(
            AgentProcessMonitorTests.CreateOrchestrator(inspector.Object),
            catalog.Object,
            new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance),
            NullLogger<AgentProcessMonitor>.Instance);
    }

    private static AgentProcessMonitor MonitorWithProcesses(Plugin[] plugins, params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector
            .Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>()))
            .Returns(processes);
        return Monitor(plugins, inspector);
    }

    private static AgentProcessInstance CreateAgentProcess(string executablePath, string? commandLine = null)
    {
        return new AgentProcessInstance(
            ProcessId: 1234,
            ExecutablePath: executablePath,
            StartTime: DateTime.UtcNow,
            CommandLine: commandLine);
    }

    private static AgentManagementService CreateService(
        Mock<IAgentDetectionService> detection,
        Mock<IAgentVersionService> version,
        Mock<IAgentCommandService> commands,
        AgentProcessMonitor monitor,
        Mock<IAgentUpdateAvailabilityService>? availability = null) =>
        new(detection.Object, version.Object, (availability ?? new Mock<IAgentUpdateAvailabilityService>()).Object, commands.Object, monitor, NullLogger<AgentManagementService>.Instance);

    private static AgentManagementService CreateService(
        IAgentDetectionService detection,
        IAgentVersionService version,
        IAgentCommandService commands,
        AgentProcessMonitor monitor,
        IAgentUpdateAvailabilityService? availability = null) =>
        new(detection, version, availability ?? new Mock<IAgentUpdateAvailabilityService>().Object, commands, monitor, NullLogger<AgentManagementService>.Instance);

    private static (AgentManagementService Service,
                    AgentDetectionService Detection,
                    AgentVersionService Version,
                    Mock<IProcessOrchestrator> Orchestrator) CreateRealService()
    {
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var orchestratorMock = new Mock<IProcessOrchestrator>();
        orchestratorMock
            .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Success(0, output: "1.0.0"));
        var detection = new AgentDetectionService(preferences, time);
        var version = new AgentVersionService(
            orchestratorMock.Object,
            preferences,
            NullLogger<AgentVersionService>.Instance,
            time);
        var service = CreateService(
            detection,
            version,
            new Mock<IAgentCommandService>().Object,
            Monitor([]),
            new Mock<IAgentUpdateAvailabilityService>().Object);

        return (service, detection, version, orchestratorMock);
    }

    [Fact]
    public async Task ProbeAsync_ReturnsInstalledStateAndVersion()
    {
        var plugin = AgentPlugin();
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(plugin)).Returns(true);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>())).ReturnsAsync("1.2.3");
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]), availability);

        var result = await service.ProbeAsync(plugin);

        Assert.True(result.IsInstalled);
        Assert.Equal("1.2.3", result.Version);
        detection.Verify(x => x.InvalidateFor(plugin), Times.Once);
        version.Verify(x => x.InvalidateFor(plugin), Times.Once);
        availability.Verify(x => x.InvalidateFor(plugin), Times.Once);
        detection.Verify(x => x.Invalidate(), Times.Never);
        version.Verify(x => x.Invalidate(), Times.Never);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }

    [Fact]
    public async Task ProbeAsync_LeavesOtherAgentsCacheServed()
    {
        var markerDir = Path.Combine(Path.GetTempPath(), "clihub-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(markerDir);

        try
        {
            var markerA = Path.Combine(markerDir, "a-marker");
            var markerB = Path.Combine(markerDir, "b-marker");
            File.WriteAllText(markerA, string.Empty);
            File.WriteAllText(markerB, string.Empty);

            var (service, detection, version, orchestrator) = CreateRealService();
            var pluginA = AgentPlugin("a");
            pluginA.Detection = new AgentDetection { SystemPaths = { markerA } };
            var pluginB = AgentPlugin("b");
            pluginB.Detection = new AgentDetection { SystemPaths = { markerB } };

            Assert.True(detection.IsInstalledInSystem(pluginA));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginA));
            Assert.True(detection.IsInstalledInSystem(pluginB));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginB));

            File.Delete(markerA);
            File.Delete(markerB);
            orchestrator
                .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
                .ReturnsAsync(ProcessResult.Success(0, output: "2.0.0"));

            var result = await service.ProbeAsync(pluginA);

            Assert.False(result.IsInstalled);

            // A's marker is gone, so the probe is gated: no version command runs for it.
            Assert.Null(result.Version);

            // B's detection and version were primed before the probe and must still be served
            // from cache even though its marker is gone and the fake output changed.
            Assert.True(detection.IsInstalledInSystem(pluginB));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginB));
            orchestrator.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Exactly(2)); // A primed, B primed; A's gated probe adds no run
        }
        finally
        {
            try { Directory.Delete(markerDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task DetectAll_InvalidatesEachAgentOnce()
    {
        var pluginA = AgentPlugin("a");
        var pluginB = AgentPlugin("b");
        var detection = new Mock<IAgentDetectionService>();
        var version = new Mock<IAgentVersionService>();
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        version
            .Setup(x => x.GetVersionAsync(It.IsAny<Plugin>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("1.0");
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]), availability);

        await service.ProbeAsync(pluginA);
        await service.ProbeAsync(pluginB);

        detection.Verify(x => x.InvalidateFor(pluginA), Times.Once);
        detection.Verify(x => x.InvalidateFor(pluginB), Times.Once);
        detection.Verify(x => x.Invalidate(), Times.Never);
        version.Verify(x => x.InvalidateFor(pluginA), Times.Once);
        version.Verify(x => x.InvalidateFor(pluginB), Times.Once);
        version.Verify(x => x.Invalidate(), Times.Never);
        availability.Verify(x => x.InvalidateFor(pluginA), Times.Once);
        availability.Verify(x => x.InvalidateFor(pluginB), Times.Once);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }

    [Fact]
    public async Task DetectAll_LeavesEveryProbedAgentCached()
    {
        var markerDir = Path.Combine(Path.GetTempPath(), "clihub-detect-all-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(markerDir);

        try
        {
            var markerA = Path.Combine(markerDir, "a-marker");
            var markerB = Path.Combine(markerDir, "b-marker");
            File.WriteAllText(markerA, string.Empty);
            File.WriteAllText(markerB, string.Empty);

            var (service, detection, version, orchestrator) = CreateRealService();
            var pluginA = AgentPlugin("a");
            pluginA.Detection = new AgentDetection { SystemPaths = { markerA } };
            var pluginB = AgentPlugin("b");
            pluginB.Detection = new AgentDetection { SystemPaths = { markerB } };

            int captureCallCount = 0;
            orchestrator
                .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
                .ReturnsAsync(() =>
                {
                    captureCallCount++;
                    return ProcessResult.Success(0, output: captureCallCount == 1 ? "1.0.0" : "2.0.0");
                });

            var resultA = await service.ProbeAsync(pluginA);
            var resultB = await service.ProbeAsync(pluginB);

            Assert.True(resultA.IsInstalled);
            Assert.Equal("1.0.0", resultA.Version);
            Assert.True(resultB.IsInstalled);
            Assert.Equal("2.0.0", resultB.Version);

            var runsAfterDetect = captureCallCount;
            Assert.Equal(2, runsAfterDetect);

            File.Delete(markerA);
            File.Delete(markerB);

            // Every probed card must stay cached: only the last card's entries surviving the
            // detect-all pass would force a re-check and a fourth version run here.
            Assert.True(detection.IsInstalledInSystem(pluginA));
            Assert.True(detection.IsInstalledInSystem(pluginB));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginA));
            Assert.Equal("2.0.0", await version.GetVersionAsync(pluginB));
            Assert.Equal(runsAfterDetect, captureCallCount);
        }
        finally
        {
            try { Directory.Delete(markerDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task ProbeAsync_UnknownVersion_ReturnsNullVersion()
    {
        var plugin = AgentPlugin();
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(plugin)).Returns(false);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]));

        var result = await service.ProbeAsync(plugin);

        Assert.False(result.IsInstalled);
        Assert.Null(result.Version);

        // The gate is the point: a not-installed agent must not get a version probe at all.
        version.Verify(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProbeAsync_InstalledAgent_StillRunsVersionCommand()
    {
        var plugin = AgentPlugin();
        var detection = new Mock<IAgentDetectionService>();
        detection.Setup(x => x.IsInstalledInSystem(plugin)).Returns(true);
        var version = new Mock<IAgentVersionService>();
        version.Setup(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>())).ReturnsAsync((string?)"4.1");
        var service = CreateService(detection, version, new Mock<IAgentCommandService>(), Monitor([]));

        var result = await service.ProbeAsync(plugin);

        Assert.True(result.IsInstalled);
        Assert.Equal("4.1", result.Version);
        version.Verify(x => x.GetVersionAsync(plugin, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_NoUpdateCommand_IsUnsupported()
    {
        var plugin = AgentPlugin(withUpdate: false);
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Unsupported, result.Outcome);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_RunningAgent_IsBlocked()
    {
        var plugin = AgentPlugin();
        var process = CreateAgentProcess(@"C:\tools\opencode.exe");
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            MonitorWithProcesses([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_OnlyOtherAgentsRunning_StartsUpdate()
    {
        var plugin = AgentPlugin("opencode");
        var other = AgentPlugin("pi");
        var otherProcess = CreateAgentProcess(@"C:\tools\pi.exe");
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            MonitorWithProcesses([plugin, other], otherProcess));

        var result = await service.UpdateAsync(plugin);

        Assert.True(result.Started);
    }

    [Fact]
    public async Task UpdateAsync_NoRunningAgent_StartsUpdate()
    {
        var plugin = AgentPlugin();
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.True(result.Started);
    }

    [Fact]
    public async Task UpdateAsync_Started_InvalidatesVersionAndAvailability()
    {
        var plugin = AgentPlugin();
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var version = new Mock<IAgentVersionService>();
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var service = CreateService(new Mock<IAgentDetectionService>(), version, commands, Monitor([]), availability);

        var result = await service.UpdateAsync(plugin);

        Assert.True(result.Started);
        version.Verify(x => x.InvalidateFor(plugin), Times.Once);
        availability.Verify(x => x.InvalidateFor(plugin), Times.Once);
        version.Verify(x => x.Invalidate(), Times.Never);
        availability.Verify(x => x.Invalidate(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Blocked_DoesNotInvalidateVersionOrAvailability()
    {
        var plugin = AgentPlugin();
        var process = CreateAgentProcess(@"C:\tools\opencode.exe");
        var version = new Mock<IAgentVersionService>();
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            version,
            new Mock<IAgentCommandService>(MockBehavior.Strict),
            MonitorWithProcesses([plugin], process),
            availability);

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        version.Verify(x => x.InvalidateFor(plugin), Times.Never);
        availability.Verify(x => x.InvalidateFor(plugin), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RuntimeHostedAgent_IsBlocked()
    {
        var plugin = AgentPlugin();
        plugin.UpdateCheck = new PluginUpdateCheck("npm", "@opencode/cli");
        var process = CreateAgentProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            MonitorWithProcesses([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        Assert.Equal("Update blocked while running", result.Message);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_UnrelatedNodeScript_StartsUpdate()
    {
        var plugin = AgentPlugin();
        var process = CreateAgentProcess(
            @"C:\Program Files\nodejs\node.exe",
            @"node.exe server.js");
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            MonitorWithProcesses([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.True(result.Started);
    }

    [Fact]
    public async Task UpdateAsync_CommandFailure_IsFailed()
    {
        var plugin = AgentPlugin();
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(false, null, "boom"));
        var service = CreateService(new Mock<IAgentDetectionService>(), new Mock<IAgentVersionService>(), commands, Monitor([]));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Failed, result.Outcome);
        Assert.Equal("boom", result.Message);
    }
}
