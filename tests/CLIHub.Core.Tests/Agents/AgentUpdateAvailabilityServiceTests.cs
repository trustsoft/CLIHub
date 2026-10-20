namespace CLIHub.Core.Tests.Agents;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Agents;
using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Tests.Fakes;

public class AgentUpdateAvailabilityServiceTests
{
    private static readonly DateTimeOffset StartTime = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

    private readonly FakeTimeProvider time = new(StartTime);
    private readonly RecordingLogger<AgentUpdateAvailabilityService> logger = new();

    private static Plugin PluginWithUpdateCheck(PluginUpdateCheck? updateCheck, string id = "agent") =>
        new() { Id = id, Name = id, UpdateCheck = updateCheck };

    private AgentUpdateAvailabilityService CreateService(
        StubVersionService? version = null,
        StubReleaseClient? release = null)
    {
        return new AgentUpdateAvailabilityService(
            version ?? new StubVersionService(),
            release ?? new StubReleaseClient(),
            new PreferencesStore(new FakeConfigurationRepository()),
            this.logger,
            this.time);
    }

    [Fact]
    public async Task Check_NoUpdateCheckBlock_ReturnsUnknownWithoutReleaseRequest()
    {
        var release = new StubReleaseClient();
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(null));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
    }

    [Fact]
    public async Task Check_UnknownSourceKind_ReturnsUnknownWithoutRequestAndLogsWarning()
    {
        var release = new StubReleaseClient();
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("cargo", Package: "some-crate")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
        Assert.Contains(this.logger.Messages, message => message.Contains("unknown update check source", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Check_NpmBlockWithoutPackage_ReturnsUnknownWithoutRequest()
    {
        var release = new StubReleaseClient();
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
        Assert.Contains(this.logger.Messages, message => message.Contains("without a package", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Check_GitHubBlockWithoutRepo_ReturnsUnknownWithoutRequest()
    {
        var release = new StubReleaseClient();
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("githubReleases")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
    }

    [Fact]
    public async Task Check_LatestGreaterThanInstalled_ReturnsUpdateAvailableWithBothVersions()
    {
        var version = new StubVersionService { Version = "opencode v2.0.32" };
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.33"));
        var service = this.CreateService(version, release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "@opencode/cli")));

        Assert.Equal(AgentUpdateStatus.UpdateAvailable, availability.Status);
        Assert.Equal("2.0.32", availability.InstalledVersion);
        Assert.Equal("2.0.33", availability.LatestVersion);
    }

    [Fact]
    public async Task Check_EqualVersions_ReturnsUpToDate()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.32"));
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline")));

        Assert.Equal(AgentUpdateStatus.UpToDate, availability.Status);
    }

    [Fact]
    public async Task Check_InstalledNewerThanLatest_ReturnsUpToDate()
    {
        var version = new StubVersionService { Version = "1.0.100" };
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("1.0.99"));
        var service = this.CreateService(version, release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("githubReleases", Repo: "github/copilot-cli")));

        Assert.Equal(AgentUpdateStatus.UpToDate, availability.Status);
    }

    [Fact]
    public async Task Check_TwoDigitMinor_ComparesNumericallyNotLexically()
    {
        var version = new StubVersionService { Version = "1.2.3" };
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("1.10.0"));
        var service = this.CreateService(version, release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "pi")));

        Assert.Equal(AgentUpdateStatus.UpdateAvailable, availability.Status);
    }

    [Fact]
    public async Task Check_UnknownInstalledVersion_SkipsReleaseRequest()
    {
        var version = new StubVersionService { Version = null };
        var release = new StubReleaseClient();
        var service = this.CreateService(version, release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
    }

    [Fact]
    public async Task Check_UnparseableInstalledVersion_SkipsReleaseRequest()
    {
        var version = new StubVersionService { Version = "no digits here" };
        var release = new StubReleaseClient();
        var service = this.CreateService(version, release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
        Assert.Equal(0, release.CallCount);
    }

    [Fact]
    public async Task Check_UnparseableLatestVersion_ReturnsUnknown()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("latest build"));
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
    }

    [Fact]
    public async Task Check_ReleaseRequestThrows_ReturnsUnknownWithoutEscaping()
    {
        var release = new StubReleaseClient(_ => throw new HttpRequestException("offline"));
        var service = this.CreateService(release: release);

        var availability = await service.CheckAsync(PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline")));

        Assert.Equal(AgentUpdateStatus.Unknown, availability.Status);
    }

    [Fact]
    public async Task Check_WithinTtl_ReusesCachedResultWithoutSecondRequest()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.33"));
        var service = this.CreateService(release: release);
        var plugin = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline"));

        var first = await service.CheckAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(10));
        var second = await service.CheckAsync(plugin);

        Assert.Equal(AgentUpdateStatus.UpdateAvailable, second.Status);
        Assert.Equal(first, second);
        Assert.Equal(1, release.CallCount);
    }

    [Fact]
    public async Task Check_AfterTtl_RequestsAgain()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.33"));
        var service = this.CreateService(release: release);
        var plugin = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline"));

        await service.CheckAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(20));

        var refreshed = await service.CheckAsync(plugin);

        Assert.Equal(AgentUpdateStatus.UpdateAvailable, refreshed.Status);
        Assert.Equal(2, release.CallCount);
    }

    [Fact]
    public async Task Check_UnknownResult_IsCachedWithinTtl()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>(null));
        var service = this.CreateService(release: release);
        var plugin = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline"));

        var first = await service.CheckAsync(plugin);
        this.time.Advance(TimeSpan.FromMinutes(10));
        var second = await service.CheckAsync(plugin);

        Assert.Equal(AgentUpdateStatus.Unknown, first.Status);
        Assert.Equal(AgentUpdateStatus.Unknown, second.Status);
        Assert.Equal(1, release.CallCount);
    }

    [Fact]
    public async Task InvalidateFor_Plugin_RequestsOnlyThatAgentAgain()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.33"));
        var service = this.CreateService(release: release);
        var pluginA = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "a"), "a");
        var pluginB = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "b"), "b");

        await service.CheckAsync(pluginA);
        await service.CheckAsync(pluginB);

        service.InvalidateFor(pluginA);

        var refreshedA = await service.CheckAsync(pluginA);
        var cachedB = await service.CheckAsync(pluginB);

        Assert.Equal(AgentUpdateStatus.UpdateAvailable, refreshedA.Status);
        Assert.Equal(AgentUpdateStatus.UpdateAvailable, cachedB.Status);
        Assert.Equal(3, release.CallCount);
    }

    [Fact]
    public async Task Invalidate_ClearsEveryCachedResult()
    {
        var release = new StubReleaseClient(_ => Task.FromResult<string?>("2.0.33"));
        var service = this.CreateService(release: release);
        var pluginA = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "a"), "a");
        var pluginB = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "b"), "b");

        await service.CheckAsync(pluginA);
        await service.CheckAsync(pluginB);

        service.Invalidate();

        await service.CheckAsync(pluginA);
        await service.CheckAsync(pluginB);

        Assert.Equal(4, release.CallCount);
    }

    [Fact]
    public async Task Check_ConcurrentChecksForOnePlugin_CoalesceIntoOneRequest()
    {
        var gate = new TaskCompletionSource();
        var release = new StubReleaseClient(async _ =>
        {
            await gate.Task;
            return "2.0.33";
        });
        var service = this.CreateService(release: release);
        var plugin = PluginWithUpdateCheck(new PluginUpdateCheck("npm", Package: "cline"));

        var first = service.CheckAsync(plugin);
        var second = service.CheckAsync(plugin);
        gate.SetResult();
        var results = await Task.WhenAll(first, second);

        Assert.All(results, availability => Assert.Equal(AgentUpdateStatus.UpdateAvailable, availability.Status));
        Assert.Equal(results[0], results[1]);
        Assert.Equal(1, release.CallCount);
    }

    private sealed class StubVersionService : IAgentVersionService
    {
        public string? Version { get; init; } = "2.0.32";

        public Task<string?> GetVersionAsync(Plugin plugin, CancellationToken cancellationToken = default) =>
            Task.FromResult(this.Version);

        public void Invalidate()
        {
        }

        public void InvalidateFor(Plugin plugin)
        {
        }
    }

    private sealed class StubReleaseClient : IAgentReleaseClient
    {
        private readonly Func<PluginUpdateCheck, Task<string?>> implementation;

        public StubReleaseClient(Func<PluginUpdateCheck, Task<string?>>? implementation = null)
        {
            this.implementation = implementation ?? (_ => Task.FromResult<string?>("2.0.33"));
        }

        public int CallCount { get; private set; }

        public Task<string?> GetLatestVersionAsync(PluginUpdateCheck updateCheck, CancellationToken cancellationToken = default)
        {
            this.CallCount++;
            return this.implementation(updateCheck);
        }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                this.Messages.Add(formatter(state, exception));
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
