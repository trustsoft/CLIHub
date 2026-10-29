namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Processes;

public class AgentCommandServiceTests : IDisposable
{
    private readonly string projectDir;
    private readonly Mock<IProcessOrchestrator> orchestratorMock = new();
    private readonly Mock<IPreferencesStore> preferencesStoreMock = new();
    private readonly Mock<IDomainEventPublisher> eventPublisherMock = new();
    private readonly AgentCommandService service;

    public AgentCommandServiceTests()
    {
        this.projectDir = Path.Combine(Path.GetTempPath(), "clihub-agent-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.projectDir);

        this.preferencesStoreMock
            .Setup(x => x.Load())
            .Returns(new AppPreferences { DefaultRuntime = RuntimeKind.PowerShell });

        this.orchestratorMock
            .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Success(0));

        this.orchestratorMock
            .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Success(0, output: "1.2.3"));

        this.service = new AgentCommandService(
            this.orchestratorMock.Object,
            this.preferencesStoreMock.Object,
            NullLogger<AgentCommandService>.Instance,
            this.eventPublisherMock.Object);
    }

    public void Dispose()
    {
        try { Directory.Delete(this.projectDir, recursive: true); } catch { }
    }

    private static Plugin PluginWith(AgentCommands commands) =>
        new() { Id = "test", Name = "Test", Commands = commands };

    [Fact]
    public async Task Version_CapturesOutput_AndDoesNotLaunchTerminal()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" },
            Version = new PluginCommand { Name = "Version", Executable = "test", Arguments = "--version" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Version, this.projectDir);

        Assert.True(result.Success);
        Assert.Equal("1.2.3", result.Output);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Once);
        this.orchestratorMock.Verify(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()), Times.Never);
    }

    [Theory]
    [InlineData(AgentCommandKind.Launch)]
    [InlineData(AgentCommandKind.Resume)]
    [InlineData(AgentCommandKind.Update)]
    public async Task InteractiveCommands_LaunchTerminal(AgentCommandKind kind)
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" },
            Resume = new PluginCommand { Name = "Resume", Executable = "test" },
            Update = new PluginCommand { Name = "Update", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, kind, this.projectDir);

        Assert.True(result.Success);
        this.orchestratorMock.Verify(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()), Times.Once);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Never);
    }

    [Fact]
    public async Task MissingCommand_ReturnsFailure()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Version, this.projectDir);

        Assert.False(result.Success);
        Assert.Contains("does not support", result.Error);
    }

    [Fact]
    public async Task MissingProjectFolder_ReturnsFailure()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Launch, "/nonexistent");

        Assert.False(result.Success);
        Assert.Contains("does not exist", result.Error);
    }

    [Fact]
    public async Task Version_NonZeroExitCode_ReturnsFailure()
    {
        this.orchestratorMock
            .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Success(1, output: "", duration: null));

        var plugin = PluginWith(new AgentCommands
        {
            Version = new PluginCommand { Name = "Version", Executable = "test", Arguments = "--version" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Version, this.projectDir);

        Assert.False(result.Success);
        Assert.Contains("Failed to get version", result.Error);
    }

    [Fact]
    public async Task AgentCommandService_SuccessfulLaunch_PublishesAgentLaunchedEvent()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Launch, this.projectDir);

        Assert.True(result.Success);
        this.eventPublisherMock.Verify(
            x => x.Publish(It.Is<AgentLaunched>(evt =>
                evt.PluginId == plugin.Id &&
                evt.ProjectPath == this.projectDir &&
                evt.CommandKind == AgentCommandKind.Launch &&
                evt.LaunchedAt <= DateTimeOffset.UtcNow &&
                evt.LaunchedAt >= DateTimeOffset.UtcNow.AddSeconds(-5))),
            Times.Once);
    }

    [Fact]
    public async Task AgentCommandService_FailedLaunch_DoesNotPublishEvent()
    {
        this.orchestratorMock
            .Setup(x => x.LaunchInteractiveAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(ProcessResult.Failure(error: "Launch failed"));

        var plugin = PluginWith(new AgentCommands
        {
            Launch = new PluginCommand { Name = "Launch", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Launch, this.projectDir);

        Assert.False(result.Success);
        this.eventPublisherMock.Verify(
            x => x.Publish(It.IsAny<AgentLaunched>()),
            Times.Never);
    }

    [Fact]
    public async Task AgentCommandService_UpdateCommand_DoesNotPublishAgentLaunchedEvent()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Update = new PluginCommand { Name = "Update", Executable = "test" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Update, this.projectDir);

        Assert.True(result.Success);
        this.eventPublisherMock.Verify(
            x => x.Publish(It.IsAny<AgentLaunched>()),
            Times.Never);
    }

    [Fact]
    public async Task AgentCommandService_VersionCommand_DoesNotPublishAgentLaunchedEvent()
    {
        var plugin = PluginWith(new AgentCommands
        {
            Version = new PluginCommand { Name = "Version", Executable = "test", Arguments = "--version" }
        });

        var result = await this.service.ExecuteAsync(plugin, AgentCommandKind.Version, this.projectDir);

        Assert.True(result.Success);
        this.eventPublisherMock.Verify(
            x => x.Publish(It.IsAny<AgentLaunched>()),
            Times.Never);
    }
}
