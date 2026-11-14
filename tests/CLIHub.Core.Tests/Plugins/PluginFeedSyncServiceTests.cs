namespace CLIHub.Core.Tests.Plugins;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

using CLIHub.Core.Configuration;
using CLIHub.Core.Events;
using CLIHub.Core.Plugins;
using CLIHub.Core.Tests.Fakes;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>
///   Covers the background feed refresh service: lifecycle (start, interval, stop, dispose), change
///   detection over the sorted <c> { id, version, compatibility } </c> tuples, and the exact
///   conditions under which <see cref="PluginFeedUpdatedEvent" /> is and is not published.
/// </summary>
public class PluginFeedSyncServiceTests : IDisposable
{
    private readonly FakeDomainEventPublisher publisher = new();
    private readonly RecordingLogger<PluginFeedSyncService> logger = new();
    private readonly string cacheDirectory = Path.Combine(
        Path.GetTempPath(),
        $"clihub-feed-sync-{Guid.NewGuid():N}");

    private string MetadataFilePath => Path.Combine(this.cacheDirectory, "feed-sync.json");

    public void Dispose()
    {
        if (Directory.Exists(this.cacheDirectory))
        {
            Directory.Delete(this.cacheDirectory, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Start_StartsTheInitialRefreshWithoutBlockingTheCaller()
    {
        var gate = new TaskCompletionSource<PluginFeedResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new StubFeedClient(_ => gate.Task);
        var service = this.CreateService(client, interval: TimeSpan.FromHours(1));

        service.Start();

        Assert.True(SpinWait.SpinUntil(() => client.CallCount == 1, TimeSpan.FromSeconds(5)));

        // The refresh is still awaiting the gate, so Start returned without waiting for the fetch.
        Assert.False(gate.Task.IsCompleted);

        gate.SetResult(Success(Index(Entry("a", "1.0.0"))));
        await service.StopAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Start_CalledTwice_KeepsASingleRefreshLoop()
    {
        var client = new StubFeedClient(_ => Task.FromResult(Success(Index(Entry("a", "1.0.0")))));
        var service = this.CreateService(client, interval: TimeSpan.FromHours(1));

        service.Start();
        service.Start();

        Assert.True(SpinWait.SpinUntil(() => client.CallCount >= 1, TimeSpan.FromSeconds(5)));
        await Task.Delay(200);

        Assert.Equal(1, client.CallCount);
        await service.StopAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Start_RefreshesAgainOnTheConfiguredInterval()
    {
        var client = new StubFeedClient(_ => Task.FromResult(Success(Index(Entry("a", "1.0.0")))));
        var service = this.CreateService(client, TimeSpan.FromMilliseconds(50));

        service.Start();

        Assert.True(SpinWait.SpinUntil(() => client.CallCount >= 3, TimeSpan.FromSeconds(10)));
        await service.StopAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task StopAsync_WithoutStart_CompletesImmediately()
    {
        var service = this.CreateService(new StubFeedClient(_ => Task.FromResult(Success(Index()))));

        await service.StopAsync(TimeSpan.FromSeconds(1));

        Assert.Empty(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task StopAsync_CancelsAnInFlightRefreshAndReturnsPromptly()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new StubFeedClient(async token =>
        {
            started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult();
                throw;
            }

            return Success(Index());
        });
        var service = this.CreateService(client, interval: TimeSpan.FromHours(1));

        service.Start();
        await started.Task;

        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        await service.StopAsync(TimeSpan.FromSeconds(10));
        elapsed.Stop();

        Assert.True(cancelled.Task.IsCompleted);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5), $"stop took {elapsed.Elapsed}");
    }

    [Fact]
    public async Task Dispose_CancelsAnInFlightRefresh()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new StubFeedClient(async token =>
        {
            started.TrySetResult();
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            catch (OperationCanceledException)
            {
                cancelled.TrySetResult();
                throw;
            }

            return Success(Index());
        });
        var service = this.CreateService(client, interval: TimeSpan.FromHours(1));

        service.Start();
        await started.Task;

        service.Dispose();

        Assert.True(cancelled.Task.IsCompleted);
    }

    [Fact]
    public async Task RefreshAsync_ChangedIndex_PublishesEventWithTheStoredHash()
    {
        var client = new StubFeedClient(_ => Task.FromResult(Success(Index(Entry("a", "1.0.0")))));
        var service = this.CreateService(client);

        await service.RefreshAsync(CancellationToken.None);

        var evt = Assert.IsType<PluginFeedUpdatedEvent>(Assert.Single(this.publisher.PublishedEvents));
        Assert.Equal(this.ReadMetadata()!.IndexHash, evt.IndexHash);
        Assert.False(string.IsNullOrWhiteSpace(evt.IndexHash));
    }

    [Fact]
    public async Task RefreshAsync_SameTuples_DoesNotPublishEvent()
    {
        var index = Index(Entry("a", "1.0.0"), Entry("b", "2.0.0"));
        this.WriteMetadata(new PluginFeedSyncMetadata { IndexHash = PluginFeedSyncMetadata.ComputeIndexHash(index.Plugins) });
        var service = this.CreateService(new StubFeedClient(_ => Task.FromResult(Success(index))));

        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_DescriptionAndOrderChangesOnly_DoesNotPublishEvent()
    {
        var stored = Index(Entry("a", "1.0.0"), Entry("b", "2.0.0"));
        this.WriteMetadata(new PluginFeedSyncMetadata { IndexHash = PluginFeedSyncMetadata.ComputeIndexHash(stored.Plugins) });

        // Reordered, and with a changed description: neither participates in change detection.
        var reordered = Index(Entry("b", "2.0.0", description: "new text"), Entry("a", "1.0.0", description: "new text"));
        var service = this.CreateService(new StubFeedClient(_ => Task.FromResult(Success(reordered))));

        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_CompatibilityChange_PublishesEvent()
    {
        var stored = Index(Entry("a", "1.0.0", compatibility: ">=1.0.0"));
        this.WriteMetadata(new PluginFeedSyncMetadata { IndexHash = PluginFeedSyncMetadata.ComputeIndexHash(stored.Plugins) });
        var changed = Index(Entry("a", "1.0.0", compatibility: ">=1.1.0"));
        var service = this.CreateService(new StubFeedClient(_ => Task.FromResult(Success(changed))));

        await service.RefreshAsync(CancellationToken.None);

        Assert.Single(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_VersionChange_PublishesEvent()
    {
        var stored = Index(Entry("a", "1.0.0"));
        this.WriteMetadata(new PluginFeedSyncMetadata { IndexHash = PluginFeedSyncMetadata.ComputeIndexHash(stored.Plugins) });
        var changed = Index(Entry("a", "1.1.0"));
        var service = this.CreateService(new StubFeedClient(_ => Task.FromResult(Success(changed))));

        await service.RefreshAsync(CancellationToken.None);

        Assert.Single(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_CacheServedFallback_DoesNotPublishEvent()
    {
        var client = new StubFeedClient(_ => Task.FromResult(
            new PluginFeedResult(true, Index(Entry("a", "1.0.0")), []) { FromCache = true, CachedAt = DateTimeOffset.UtcNow }));
        var service = this.CreateService(client);

        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
        Assert.Null(this.ReadMetadata());
    }

    [Fact]
    public async Task RefreshAsync_FailedFetch_LogsWarningAndDoesNotPublishEvent()
    {
        var client = new StubFeedClient(_ => Task.FromResult(
            new PluginFeedResult(false, null, ["Could not load plugin feed. Check your network connection."])));
        var service = this.CreateService(client);

        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
        Assert.Contains(this.logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task RefreshAsync_ThrownFetchException_IsLoggedAtWarningAndNotPropagated()
    {
        var client = new StubFeedClient(_ => throw new HttpRequestException("offline"));
        var service = this.CreateService(client);

        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
        Assert.Contains(this.logger.Entries, entry => entry.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task RefreshAsync_FeedDisabled_SkipsTheFetchAndPublishesNothing()
    {
        var client = new StubFeedClient(_ => Task.FromResult(Success(Index(Entry("a", "1.0.0")))));
        var service = this.CreateService(client, feedUrl: string.Empty);

        await service.RefreshAsync(CancellationToken.None);

        Assert.Equal(0, client.CallCount);
        Assert.Empty(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_Cancelled_PropagatesCancellationWithoutPublishing()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var client = new StubFeedClient(token =>
        {
            token.ThrowIfCancellationRequested();
            return Task.FromResult(Success(Index()));
        });
        var service = this.CreateService(client);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => service.RefreshAsync(cancellation.Token));

        Assert.Empty(this.publisher.PublishedEvents);
    }

    [Fact]
    public async Task RefreshAsync_AfterAClientOnlyFreshFetch_MatchesTheClientsHashAndPublishesNothingOn304()
    {
        // Verifier finding a: a Browse-style fetch may cache before the sync service ever runs. The
        // client now writes the comparison hash itself, so the service's first conditional request
        // (304) compares equal and publishes no change event.
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        preferences.Update(p => p.PluginFeedUrl = FeedUrlProvider.DefaultFeedUrl);
        var handler = new QueueHandler(
            WithEtag(JsonResponse(IndexJson), "\"v1\""),
            new HttpResponseMessage(HttpStatusCode.NotModified));
        var client = new PluginFeedClient(
            handler,
            preferences,
            NullLogger<PluginFeedClient>.Instance,
            cacheFilePath: Path.Combine(this.cacheDirectory, "feed-index.json"),
            syncMetadataFilePath: this.MetadataFilePath);

        var direct = await client.GetFeedAsync();
        Assert.True(direct.Success);
        Assert.NotNull(this.ReadMetadata()!.IndexHash);

        var service = new PluginFeedSyncService(
            client,
            this.publisher,
            preferences,
            this.logger,
            timeProvider: null,
            syncMetadataFilePath: this.MetadataFilePath,
            interval: null);
        await service.RefreshAsync(CancellationToken.None);

        Assert.Empty(this.publisher.PublishedEvents);
        Assert.Equal(2, handler.RequestCount);
        Assert.Equal("\"v1\"", Assert.Single(handler.Requests[1].Headers.GetValues("If-None-Match")));
    }

    private PluginFeedSyncService CreateService(
        IPluginFeedClient client,
        TimeSpan? interval = null,
        string? feedUrl = null)
    {
        var preferences = new PreferencesStore(new FakeConfigurationRepository());
        preferences.Update(p => p.PluginFeedUrl = feedUrl ?? FeedUrlProvider.DefaultFeedUrl);

        return new PluginFeedSyncService(
            client,
            this.publisher,
            preferences,
            this.logger,
            timeProvider: null,
            syncMetadataFilePath: this.MetadataFilePath,
            interval: interval);
    }

    private PluginFeedSyncMetadata? ReadMetadata() => PluginFeedSyncMetadata.Load(this.MetadataFilePath, this.logger);

    private void WriteMetadata(PluginFeedSyncMetadata metadata)
    {
        Directory.CreateDirectory(this.cacheDirectory);
        metadata.Save(this.MetadataFilePath);
    }

    private static PluginFeedResult Success(PluginFeedIndex index) => new(true, index, []);

    private const string IndexJson = """
        {"schemaVersion":1,"plugins":[
          {"id":"plugin-a","name":"Plugin A","description":"D","version":"1.0.0","schemaVersion":1,
           "minAppVersion":"1.0.0","publisher":"P","homepage":"https://e.com",
           "packageUrl":"https://e.com/a.zip","sha256":"abc"}
        ]}
        """;

    private static PluginFeedIndex Index(params PluginFeedEntry[] entries) => new(2, null, entries);

    private static PluginFeedEntry Entry(
        string id,
        string version,
        string compatibility = ">=1.0.0",
        string description = "description") =>
        new(
            id,
            id,
            description,
            version,
            1,
            null,
            "Publisher",
            "https://example.com",
            null,
            null,
            null,
            null,
            compatibility,
            "stable",
            "{}",
            null);

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static HttpResponseMessage WithEtag(HttpResponseMessage response, string etag)
    {
        response.Headers.ETag = new EntityTagHeaderValue(etag);
        return response;
    }

    private sealed class QueueHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses;

        public QueueHandler(params HttpResponseMessage[] responses) =>
            this.responses = new Queue<HttpResponseMessage>(responses);

        public List<HttpRequestMessage> Requests { get; } = [];

        public int RequestCount => this.Requests.Count;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Requests.Add(request);
            return Task.FromResult(this.responses.Dequeue());
        }
    }

    private sealed class StubFeedClient : IPluginFeedClient
    {
        private readonly Func<CancellationToken, Task<PluginFeedResult>> fetch;
        private int callCount;

        public StubFeedClient(Func<CancellationToken, Task<PluginFeedResult>> fetch) => this.fetch = fetch;

        public int CallCount => Volatile.Read(ref this.callCount);

        public Task<PluginFeedResult> GetFeedAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref this.callCount);
            return this.fetch(cancellationToken);
        }

        public Task<PluginDownloadResult> DownloadPackageAsync(string packageUrl, string expectedSha256, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PluginLogoDownloadResult> DownloadLogoAsync(string logoUrl, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        private readonly ConcurrentQueue<(LogLevel Level, string Message)> entries = new();

        public IReadOnlyCollection<(LogLevel Level, string Message)> Entries => this.entries;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            this.entries.Enqueue((logLevel, formatter(state, exception)));
    }
}
