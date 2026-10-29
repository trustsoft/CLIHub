namespace CLIHub.Core.Tests.Processes;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using CLIHub.Core.Infrastructure.Logging;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

/// <summary>
///   Unit tests for <see cref="ProcessOrchestrator" />.
/// </summary>
public class ProcessOrchestratorTests
{
    private readonly Mock<IRuntimeResolver> mockResolver;
    private readonly Mock<ICommandLineBuilder> mockBuilder;
    private readonly Mock<IInteractiveProcessRunner> mockInteractiveRunner;
    private readonly Mock<IProcessOutputRunner> mockOutputRunner;
    private readonly Mock<IAgentProcessFinder> mockFinder;
    private readonly ProcessOrchestrator orchestrator;

    public ProcessOrchestratorTests()
    {
        this.mockResolver = new Mock<IRuntimeResolver>();
        this.mockBuilder = new Mock<ICommandLineBuilder>();
        this.mockInteractiveRunner = new Mock<IInteractiveProcessRunner>();
        this.mockOutputRunner = new Mock<IProcessOutputRunner>();
        this.mockFinder = new Mock<IAgentProcessFinder>();
        var loggingContext = new LoggingContextProvider(NullLogger<LoggingContextProvider>.Instance);
        this.orchestrator = new ProcessOrchestrator(
            this.mockResolver.Object,
            this.mockBuilder.Object,
            this.mockInteractiveRunner.Object,
            this.mockOutputRunner.Object,
            this.mockFinder.Object,
            loggingContext,
            NullLogger<ProcessOrchestrator>.Instance);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_Success_RecordsExitCodeAndDuration()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "powershell.exe", Kind = RuntimeKind.PowerShell };
        var command = new PluginCommand { Name = "test", Executable = "agent.exe", Arguments = "--run" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.PowerShell);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.PowerShell, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildInteractive(runtime, command, @"C:\work"))
            .Returns(("powershell.exe", "-Command agent.exe --run"));
        this.mockInteractiveRunner.Setup(x => x.LaunchAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), @"C:\work", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        var result = await this.orchestrator.LaunchInteractiveAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.NotNull(result.Duration);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_RuntimeUnavailable_ReturnsFailure()
    {
        // Arrange
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.GitBash);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.GitBash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RuntimeExecutable?)null);

        // Act
        var result = await this.orchestrator.LaunchInteractiveAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Failure, result.Kind);
        Assert.Contains("not available", result.Error);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_RunnerException_ReturnsFailureWithMessage()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "cmd.exe", Kind = RuntimeKind.CommandPrompt };
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.CommandPrompt);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.CommandPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildInteractive(runtime, command, @"C:\work"))
            .Returns(("cmd.exe", "/c agent.exe"));
        this.mockInteractiveRunner.Setup(x => x.LaunchAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), @"C:\work", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Process launch failed"));

        // Act
        var result = await this.orchestrator.LaunchInteractiveAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Failure, result.Kind);
        Assert.Contains("Process launch failed", result.Error);
    }

    [Fact]
    public async Task LaunchInteractiveAsync_Cancelled_ReturnsCancelled()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "powershell.exe", Kind = RuntimeKind.PowerShell };
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };
        using var cts = new CancellationTokenSource();
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.PowerShell, cts.Token);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.PowerShell, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildInteractive(runtime, command, @"C:\work"))
            .Returns(("powershell.exe", "-Command agent.exe"));
        this.mockInteractiveRunner.Setup(x => x.LaunchAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), @"C:\work", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await this.orchestrator.LaunchInteractiveAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Cancelled, result.Kind);
    }

    [Fact]
    public async Task CaptureOutputAsync_Success_ReturnsTrimmedOutputAndExitCode()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "cmd.exe", Kind = RuntimeKind.CommandPrompt };
        var command = new PluginCommand { Name = "test", Executable = "echo.exe", Arguments = "hello" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.CommandPrompt);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.CommandPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildCaptured("echo.exe", "hello"))
            .Returns(("cmd.exe", "/c echo hello"));
        this.mockOutputRunner.Setup(x => x.RunAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CLIHub.Core.Infrastructure.Processes.ProcessResult
            {
                StandardOutput = "  hello-output  ",
                StandardError = string.Empty,
                ExitCode = 0,
                TimedOut = false
            });

        // Act
        var result = await this.orchestrator.CaptureOutputAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("hello-output", result.Output);
        Assert.NotNull(result.Duration);
    }

    [Fact]
    public async Task CaptureOutputAsync_TimedOut_ReturnsTimeout()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "cmd.exe", Kind = RuntimeKind.CommandPrompt };
        var command = new PluginCommand { Name = "test", Executable = "ping.exe", Arguments = "-n 10 127.0.0.1" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.CommandPrompt);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.CommandPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildCaptured("ping.exe", "-n 10 127.0.0.1"))
            .Returns(("cmd.exe", "/c ping -n 10 127.0.0.1"));
        this.mockOutputRunner.Setup(x => x.RunAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CLIHub.Core.Infrastructure.Processes.ProcessResult
            {
                StandardOutput = string.Empty,
                StandardError = string.Empty,
                ExitCode = -1,
                TimedOut = true
            });

        // Act
        var result = await this.orchestrator.CaptureOutputAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Timeout, result.Kind);
    }

    [Fact]
    public async Task CaptureOutputAsync_Exception_ReturnsFailure()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "cmd.exe", Kind = RuntimeKind.CommandPrompt };
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.CommandPrompt);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.CommandPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildCaptured("agent.exe", null))
            .Returns(("cmd.exe", "/c agent.exe"));
        this.mockOutputRunner.Setup(x => x.RunAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Process did not start"));

        // Act
        var result = await this.orchestrator.CaptureOutputAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Failure, result.Kind);
        Assert.Contains("Process did not start", result.Error);
    }

    [Fact]
    public async Task CaptureOutputAsync_Cancelled_ReturnsCancelled()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "cmd.exe", Kind = RuntimeKind.CommandPrompt };
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };
        using var cts = new CancellationTokenSource();
        var request = LaunchRequest.Create(@"C:\work", command, RuntimeKind.CommandPrompt, cts.Token);

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.CommandPrompt, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);
        this.mockBuilder.Setup(x => x.BuildCaptured("agent.exe", null))
            .Returns(("cmd.exe", "/c agent.exe"));
        this.mockOutputRunner.Setup(x => x.RunAsync(
                It.IsAny<string>(), It.IsAny<RuntimeInfo>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await this.orchestrator.CaptureOutputAsync(request);

        // Assert
        Assert.Equal(ProcessResultKind.Cancelled, result.Kind);
    }

    [Fact]
    public async Task FindRunningAgentsAsync_SinglePlugin_DelegatesToFinder()
    {
        // Arrange
        var plugin = new Plugin
        {
            Id = "test-plugin",
            Name = "Test",
            Commands = new AgentCommands { Launch = new PluginCommand { Name = "launch", Executable = "agent.exe" } }
        };
        var criteria = ProcessMatchCriteria.Create(plugin, AgentProcessMatchDetail.CommandLine);
        var expected = new List<AgentProcess>
        {
            new()
            {
                ProcessId = 1234,
                ExecutablePath = @"C:\agent.exe",
                CommandLine = "agent.exe --run",
                StartTime = DateTime.UtcNow
            }
        };

        this.mockFinder.Setup(x => x.FindAsync(criteria, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await this.orchestrator.FindRunningAgentsAsync(criteria);

        // Assert
        Assert.Single(result);
        Assert.Equal(1234, result[0].ProcessId);
    }

    [Fact]
    public async Task FindRunningAgentsAsync_BatchedPlugins_DelegatesToFinder()
    {
        // Arrange
        var plugins = new List<Plugin>
        {
            new()
            {
                Id = "plugin-1",
                Name = "P1",
                Commands = new AgentCommands { Launch = new PluginCommand { Name = "launch", Executable = "p1.exe" } }
            },
            new()
            {
                Id = "plugin-2",
                Name = "P2",
                Commands = new AgentCommands { Launch = new PluginCommand { Name = "launch", Executable = "p2.exe" } }
            }
        };
        var p1Process = new AgentProcess
        {
            ProcessId = 100,
            ExecutablePath = @"C:\p1.exe",
            CommandLine = "p1.exe",
            StartTime = DateTime.UtcNow
        };
        var expected = new List<PluginAgentProcess>
        {
            new(plugins[0], p1Process)
        };

        this.mockFinder.Setup(x => x.FindAllAsync(plugins, AgentProcessMatchDetail.ExecutableName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await this.orchestrator.FindRunningAgentsAsync(plugins, AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.Single(result);
        Assert.Equal("plugin-1", result[0].Plugin.Id);
    }

    [Fact]
    public async Task ResolveRuntimeAsync_DelegatesToResolver()
    {
        // Arrange
        var runtime = new RuntimeExecutable { ExecutablePath = "powershell.exe", Kind = RuntimeKind.PowerShell };

        this.mockResolver.Setup(x => x.ResolveAsync(RuntimeKind.PowerShell, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runtime);

        // Act
        var result = await this.orchestrator.ResolveRuntimeAsync(RuntimeKind.PowerShell);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("powershell.exe", result.ExecutablePath);
    }
}

