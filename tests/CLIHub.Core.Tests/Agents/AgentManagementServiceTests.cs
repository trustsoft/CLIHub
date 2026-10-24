namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Plugins;
using CLIHub.Core.Tests.Fakes;

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

    private static AgentProcessMonitor Monitor(Plugin[] plugins, params AgentProcessInstance[] processes)
    {
        var inspector = new Mock<IAgentProcessInspector>();
        inspector.Setup(x => x.GetRunningProcesses(It.IsAny<IReadOnlySet<string>>(), It.IsAny<AgentProcessMatchDetail>())).Returns(processes);

        var catalog = new Mock<IPluginCatalog>();
        catalog.Setup(x => x.GetAllPlugins()).Returns(plugins);

        return new AgentProcessMonitor(
            inspector.Object,
            new AgentProcessMatcher(),
            catalog.Object,
            NullLogger<AgentProcessMonitor>.Instance);
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

    private static ProcessResult Output(string standardOutput) => new()
    {
        StandardOutput = standardOutput,
        StandardError = string.Empty,
        ExitCode = 0,
        TimedOut = false
    };

    private static (AgentManagementService Service,
                    AgentDetectionService Detection,
                    AgentVersionService Version,
                    FakeProcessOutputRunner OutputRunner) CreateRealService()
    {
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var outputRunner = new FakeProcessOutputRunner { Result = Output("1.0.0") };
        var launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new FakeInteractiveProcessRunner(),
            outputRunner,
            new FakeRuntimeExecutableLocator());
        var detection = new AgentDetectionService(preferences, time);
        var version = new AgentVersionService(launcher, preferences, NullLogger<AgentVersionService>.Instance, time);
        var service = CreateService(
            detection,
            version,
            new Mock<IAgentCommandService>().Object,
            Monitor([]),
            new Mock<IAgentUpdateAvailabilityService>().Object);

        return (service, detection, version, outputRunner);
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

            var (service, detection, version, outputRunner) = CreateRealService();
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
            outputRunner.Result = Output("2.0.0");

            var result = await service.ProbeAsync(pluginA);

            Assert.False(result.IsInstalled);
            Assert.Equal("2.0.0", result.Version);

            // B's detection and version were primed before the probe and must still be served
            // from cache even though its marker is gone and the fake output changed.
            Assert.True(detection.IsInstalledInSystem(pluginB));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginB));
            Assert.Equal(3, outputRunner.Runs.Count); // A primed, B primed, A re-probed
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

            var (service, detection, version, outputRunner) = CreateRealService();
            var pluginA = AgentPlugin("a");
            pluginA.Detection = new AgentDetection { SystemPaths = { markerA } };
            var pluginB = AgentPlugin("b");
            pluginB.Detection = new AgentDetection { SystemPaths = { markerB } };

            outputRunner.Result = Output("1.0.0");
            var resultA = await service.ProbeAsync(pluginA);
            outputRunner.Result = Output("2.0.0");
            var resultB = await service.ProbeAsync(pluginB);

            Assert.True(resultA.IsInstalled);
            Assert.Equal("1.0.0", resultA.Version);
            Assert.True(resultB.IsInstalled);
            Assert.Equal("2.0.0", resultB.Version);

            var runsAfterDetect = outputRunner.Runs.Count;
            Assert.Equal(2, runsAfterDetect);

            File.Delete(markerA);
            File.Delete(markerB);

            // Every probed card must stay cached: only the last card's entries surviving the
            // detect-all pass would force a re-check and a fourth version run here.
            Assert.True(detection.IsInstalledInSystem(pluginA));
            Assert.True(detection.IsInstalledInSystem(pluginB));
            Assert.Equal("1.0.0", await version.GetVersionAsync(pluginA));
            Assert.Equal("2.0.0", await version.GetVersionAsync(pluginB));
            Assert.Equal(runsAfterDetect, outputRunner.Runs.Count);
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
        var process = new AgentProcessInstance(1234, @"C:\tools\opencode.exe", DateTime.UtcNow);
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            Monitor([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_OnlyOtherAgentsRunning_StartsUpdate()
    {
        var plugin = AgentPlugin("opencode");
        var other = AgentPlugin("pi");
        var otherProcess = new AgentProcessInstance(1234, @"C:\tools\pi.exe", DateTime.UtcNow);
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            Monitor([plugin, other], otherProcess));

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
        var process = new AgentProcessInstance(1234, @"C:\tools\opencode.exe", DateTime.UtcNow);
        var version = new Mock<IAgentVersionService>();
        var availability = new Mock<IAgentUpdateAvailabilityService>();
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            version,
            new Mock<IAgentCommandService>(MockBehavior.Strict),
            Monitor([plugin], process),
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
        var process = new AgentProcessInstance(
            1234,
            @"C:\Program Files\nodejs\node.exe",
            DateTime.UtcNow,
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run");
        var commands = new Mock<IAgentCommandService>(MockBehavior.Strict);
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            Monitor([plugin], process));

        var result = await service.UpdateAsync(plugin);

        Assert.Equal(AgentUpdateOutcome.Blocked, result.Outcome);
        Assert.Contains(plugin.Name, result.Message, StringComparison.Ordinal);
        commands.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_UnrelatedNodeScript_StartsUpdate()
    {
        var plugin = AgentPlugin();
        var process = new AgentProcessInstance(
            1234,
            @"C:\Program Files\nodejs\node.exe",
            DateTime.UtcNow,
            @"node.exe server.js");
        var commands = new Mock<IAgentCommandService>();
        commands
            .Setup(x => x.ExecuteAsync(plugin, AgentCommandKind.Update, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AgentCommandResult(true, null, null));
        var service = CreateService(
            new Mock<IAgentDetectionService>(),
            new Mock<IAgentVersionService>(),
            commands,
            Monitor([plugin], process));

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
