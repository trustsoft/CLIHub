namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging.Abstractions;
using Moq;

using CLIHub.Core.Models;
using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Processes;
using CLIHub.Core.Tests.Fakes;

public class AgentVersionServiceTests
{
    private readonly Mock<IProcessOrchestrator> orchestratorMock = new();
    private readonly FakeConfigurationRepository config = new();
    private readonly FakeTimeProvider time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private ProcessResult outputResult = ProcessResult.Success(0, output: "1.2.3");

    public AgentVersionServiceTests()
    {
        this.orchestratorMock
            .Setup(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()))
            .ReturnsAsync(() => this.outputResult);
    }

    private AgentVersionService CreateService() =>
        new(
            this.orchestratorMock.Object,
            new PreferencesStore(this.config),
            NullLogger<AgentVersionService>.Instance,
            this.time);

    private static Plugin PluginWithVersion(bool includeVersion, string id = "agent")
    {
        var commands = new AgentCommands
        {
            Launch = new PluginCommand { Executable = "agent" }
        };
        if (includeVersion)
        {
            commands.Version = new PluginCommand { Executable = "agent", Arguments = "--version" };
        }

        return new Plugin { Id = id, Name = id, Commands = commands };
    }

    [Fact]
    public async Task GetVersion_CommandDefined_ReturnsFirstLine()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.2.3\nsome banner");
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("1.2.3", version);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Once);
    }

    [Theory]
    [InlineData("GitHub Copilot CLI 1.0.88.", "1.0.88")]
    [InlineData("0.30.0 (OpenClaude)", "0.30.0")]
    [InlineData("1.18.32", "1.18.32")]
    [InlineData("v2.5", "2.5")]
    public async Task GetVersion_ExtractsVersionNumber(string output, string expected)
    {
        this.outputResult = ProcessResult.Success(0, output: output);
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal(expected, version);
    }

    [Fact]
    public async Task GetVersion_NoVersionNumber_ReturnsLineAsIs()
    {
        this.outputResult = ProcessResult.Success(0, output: "no digits here");
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Equal("no digits here", version);
    }

    [Fact]
    public async Task GetVersion_NoVersionCommand_ReturnsNullAndDoesNotRun()
    {
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(false));

        Assert.Null(version);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Never);
    }

    [Fact]
    public async Task GetVersion_CaptureFailure_ReturnsNull()
    {
        this.outputResult = ProcessResult.Success(1, output: string.Empty);
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_Timeout_ReturnsNull()
    {
        this.outputResult = ProcessResult.Timeout();
        var service = this.CreateService();

        var version = await service.GetVersionAsync(PluginWithVersion(true));

        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_WithinCacheTtl_ReturnsCachedVersionWithoutRunning()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.2.3");
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(10));
        var cached = await service.GetVersionAsync(plugin);

        Assert.Equal("1.2.3", cached);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Once);
    }

    [Fact]
    public async Task GetVersion_AfterCacheExpiry_RunsAgain()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.2.3");
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(20)); // Default TTL is 15 minutes

        this.outputResult = ProcessResult.Success(0, output: "2.0.0");

        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Invalidate_ClearsCachedVersions()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.2.3");
        var service = this.CreateService();
        var plugin = PluginWithVersion(true);

        await service.GetVersionAsync(plugin);
        service.Invalidate();

        this.outputResult = ProcessResult.Success(0, output: "2.0.0");
        
        var version = await service.GetVersionAsync(plugin);

        Assert.Equal("2.0.0", version);
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateFor_Plugin_RunsOnlyThatAgentsVersionCommandAgain()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.0.0");
        var service = this.CreateService();
        var pluginA = PluginWithVersion(true, "a");
        var pluginB = PluginWithVersion(true, "b");

        Assert.Equal("1.0.0", await service.GetVersionAsync(pluginA));
        Assert.Equal("1.0.0", await service.GetVersionAsync(pluginB));

        this.outputResult = ProcessResult.Success(0, output: "2.0.0");
        service.InvalidateFor(pluginA);

        Assert.Equal("2.0.0", await service.GetVersionAsync(pluginA));
        Assert.Equal("1.0.0", await service.GetVersionAsync(pluginB));
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Exactly(3));
    }

    [Fact]
    public async Task InvalidateFor_UnknownPlugin_DoesNotClearExistingEntries()
    {
        this.outputResult = ProcessResult.Success(0, output: "1.0.0");
        var service = this.CreateService();
        var plugin = PluginWithVersion(true, "a");

        Assert.Equal("1.0.0", await service.GetVersionAsync(plugin));

        this.outputResult = ProcessResult.Success(0, output: "2.0.0");
        service.InvalidateFor(PluginWithVersion(true, "unknown"));

        Assert.Equal("1.0.0", await service.GetVersionAsync(plugin));
        this.orchestratorMock.Verify(x => x.CaptureOutputAsync(It.IsAny<LaunchRequest>()), Times.Once);
    }
}
