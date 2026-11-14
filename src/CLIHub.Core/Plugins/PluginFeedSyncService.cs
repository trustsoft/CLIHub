namespace CLIHub.Core.Plugins;

using CLIHub.Core.Configuration;
using CLIHub.Core.Events;

using Microsoft.Extensions.Logging;

/// <summary>
///   Runs the plugin feed background refresh: one refresh at start, then one per fixed interval,
///   with conditional requests handled by <see cref="IPluginFeedClient" /> and change detection over
///   the cached index hash. Refreshes are serialized, failures are logged at Warning and never
///   thrown, and the feed is re-checked on every refresh so a disabled feed is skipped rather than
///   fetched.
/// </summary>
public sealed class PluginFeedSyncService : IPluginFeedSyncService, IDisposable
{
    /// <summary>
    ///   The refresh interval. Fixed in this slice; a later slice can surface it as a preference
    ///   without restructuring the loop.
    /// </summary>
    internal static readonly TimeSpan DefaultSyncInterval = TimeSpan.FromHours(1);

    /// <summary>
    ///   The maximum time disposal waits for an in-flight refresh, matching the application's
    ///   shutdown budget.
    /// </summary>
    private static readonly TimeSpan DisposalStopTimeout = TimeSpan.FromSeconds(2);

    private readonly IPluginFeedClient feedClient;
    private readonly IDomainEventPublisher publisher;
    private readonly IPreferencesStore preferences;
    private readonly ILogger<PluginFeedSyncService> logger;
    private readonly TimeProvider time;
    private readonly string syncMetadataFilePath;
    private readonly TimeSpan interval;

    private readonly object gate = new();
    private CancellationTokenSource? cancellation;
    private Task? loop;
    private bool started;
    private bool disposed;

    /// <summary>
    ///   Creates the sync service.
    /// </summary>
    /// <param name="feedClient"> Feed client; conditional requests and the cache live there. </param>
    /// <param name="publisher"> Event publisher for <see cref="PluginFeedUpdatedEvent" />. </param>
    /// <param name="preferences"> Preferences store, re-read on every refresh to honor a disabled feed. </param>
    /// <param name="logger"> Logger; refresh failures are logged at Warning. </param>
    public PluginFeedSyncService(IPluginFeedClient feedClient,
                                 IDomainEventPublisher publisher,
                                 IPreferencesStore preferences,
                                 ILogger<PluginFeedSyncService> logger)
        : this(feedClient, publisher, preferences, logger, null, null, null)
    {
    }

    /// <summary>
    ///   Creates the sync service with the time source, metadata path, and interval overridden for
    ///   tests. Production composition uses the four-argument constructor.
    /// </summary>
    /// <param name="feedClient"> Feed client. </param>
    /// <param name="publisher"> Event publisher. </param>
    /// <param name="preferences"> Preferences store. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="timeProvider"> Time source for the interval wait; system time when null. </param>
    /// <param name="syncMetadataFilePath"> Overrides the metadata path; defaults to <c> AppPaths.FeedSyncMetadataFile </c>. </param>
    /// <param name="interval"> Overrides the refresh interval; defaults to one hour. </param>
    internal PluginFeedSyncService(IPluginFeedClient feedClient,
                                   IDomainEventPublisher publisher,
                                   IPreferencesStore preferences,
                                   ILogger<PluginFeedSyncService> logger,
                                   TimeProvider? timeProvider,
                                   string? syncMetadataFilePath,
                                   TimeSpan? interval)
    {
        this.feedClient = feedClient ?? throw new ArgumentNullException(nameof(feedClient));
        this.publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.time = timeProvider ?? TimeProvider.System;
        this.syncMetadataFilePath = string.IsNullOrWhiteSpace(syncMetadataFilePath)
            ? Infrastructure.FileSystem.AppPaths.FeedSyncMetadataFile
            : syncMetadataFilePath;
        this.interval = interval ?? DefaultSyncInterval;
    }

    /// <inheritdoc />
    public void Start()
    {
        lock (this.gate)
        {
            if (this.disposed || this.started)
            {
                return;
            }

            this.started = true;
            this.cancellation = new CancellationTokenSource();
            var token = this.cancellation.Token;

            this.logger.LogInformation("Plugin feed background sync started (interval {Interval})", this.interval);

            // The loop owns its failures; Task.Run keeps the first refresh off the caller's thread
            // so startup and the UI never wait for the network.
            this.loop = Task.Run(() => this.RunLoopAsync(token), CancellationToken.None);
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(TimeSpan timeout)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(timeout, TimeSpan.Zero);

        Task? loop;
        CancellationTokenSource? source;

        lock (this.gate)
        {
            loop = this.loop;
            source = this.cancellation;
            this.loop = null;
            this.cancellation = null;
            this.started = false;
        }

        if (source is null)
        {
            return;
        }

        source.Cancel();

        try
        {
            if (loop is not null)
            {
                await loop.WaitAsync(timeout).ConfigureAwait(false);
            }

            this.logger.LogInformation("Plugin feed background sync stopped");
        }
        catch (TimeoutException)
        {
            this.logger.LogWarning(
                "Plugin feed background sync did not stop within {Timeout}; abandoning the in-flight refresh",
                timeout);
        }
        catch (OperationCanceledException)
        {
            // The loop exited through the cancelled token; that is the expected stop path.
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Plugin feed background sync failed while stopping");
        }
        finally
        {
            source.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (this.gate)
        {
            if (this.disposed)
            {
                return;
            }

            this.disposed = true;
        }

        try
        {
            // Shutdown budget: the in-flight refresh is cancelled and awaited only briefly, so a
            // hung network call can never delay application exit.
            this.StopAsync(DisposalStopTimeout).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Plugin feed background sync disposal failed");
        }
    }

    /// <summary>
    ///   Runs one refresh immediately and then once per interval until cancelled. Every iteration is
    ///   independently guarded, so a failing refresh never ends the loop; cancellation is the only
    ///   exit.
    /// </summary>
    /// <param name="cancellationToken"> The loop's cancellation token. </param>
    /// <returns> A task completing when the loop exits. </returns>
    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await this.RefreshAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Background plugin feed refresh failed unexpectedly");
            }

            try
            {
                await Task.Delay(this.interval, this.time, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    ///   Performs one background refresh: skips when the feed is disabled, fetches conditionally,
    ///   skips cache-served and failed results, computes the comparison hash, and publishes
    ///   <see cref="PluginFeedUpdatedEvent" /> after persisting the new hash when it changed. A 304
    ///   publishes nothing because the hash of the cached index equals the stored hash.
    /// </summary>
    /// <param name="cancellationToken"> Cancellation token for the fetch. </param>
    /// <returns> A task completing when the refresh finishes. </returns>
    internal async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (FeedUrlProvider.GetFeedUrl(this.preferences) is null)
        {
            this.logger.LogDebug("Plugin feed is disabled in configuration; skipping the background refresh");
            return;
        }

        var previousHash = PluginFeedSyncMetadata.Load(this.syncMetadataFilePath, this.logger)?.IndexHash;

        PluginFeedResult result;
        try
        {
            result = await this.feedClient.GetFeedAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Background plugin feed refresh failed");
            return;
        }

        if (!result.Success || result.Index is null)
        {
            this.logger.LogWarning(
                "Background plugin feed refresh failed: {Errors}",
                string.Join("; ", result.Errors));
            return;
        }

        if (result.FromCache)
        {
            // The live fetch failed and the client served stale data: never a change signal.
            this.logger.LogDebug("Background plugin feed refresh served the cached index; no event");
            return;
        }

        var indexHash = PluginFeedSyncMetadata.ComputeIndexHash(result.Index.Plugins);
        if (string.Equals(indexHash, previousHash, StringComparison.Ordinal))
        {
            this.logger.LogDebug("Background plugin feed refresh found no change");
            return;
        }

        this.SaveIndexHash(indexHash);
        this.publisher.Publish(new PluginFeedUpdatedEvent(indexHash));
        this.logger.LogInformation("Plugin feed changed; published the update event");
    }

    /// <summary>
    ///   Stores the comparison hash in the sync metadata, preserving the HTTP validators the client
    ///   wrote, so the next refresh compares against it. A write failure only repeats the event on
    ///   the next refresh, so it is logged rather than thrown.
    /// </summary>
    /// <param name="indexHash"> The new comparison hash. </param>
    private void SaveIndexHash(string indexHash)
    {
        try
        {
            var metadata = PluginFeedSyncMetadata.Load(this.syncMetadataFilePath, this.logger)
                           ?? new PluginFeedSyncMetadata();
            (metadata with { IndexHash = indexHash }).Save(this.syncMetadataFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            this.logger.LogWarning(ex, "Could not write the plugin feed index hash to {Path}", this.syncMetadataFilePath);
        }
    }
}
