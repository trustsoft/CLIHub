namespace CLIHub.Core.Agents;

using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;
using CLIHub.Core.Models.Plugins;

/// <summary>
///   Compares an agent's installed version with the version published by its declared source,
///   with per-agent caching that expires after the shared agent probe time-to-live.
///   Every failure path degrades to <see cref="AgentUpdateStatus.Unknown" />; the result never
///   gates or alters the Update action.
/// </summary>
public class AgentUpdateAvailabilityService : IAgentUpdateAvailabilityService
{
    private readonly IAgentVersionService versionService;
    private readonly IAgentReleaseClient releaseClient;
    private readonly IPreferencesStore preferencesStore;
    private readonly ILogger<AgentUpdateAvailabilityService> logger;
    private readonly TimeProvider timeProvider;
    private readonly ConcurrentDictionary<string, CacheEntry> cache = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<AgentUpdateAvailability>>> inFlight = new();

    /// <summary>
    ///   Creates the service.
    /// </summary>
    /// <param name="versionService"> Installed-version probe service. </param>
    /// <param name="releaseClient"> Latest-published-version reader. </param>
    /// <param name="preferencesStore"> Preference store supplying the TTL. </param>
    /// <param name="logger"> Logger. </param>
    /// <param name="timeProvider"> Time source for cache expiry, or null for the system clock. </param>
    public AgentUpdateAvailabilityService(IAgentVersionService versionService,
                                          IAgentReleaseClient releaseClient,
                                          IPreferencesStore preferencesStore,
                                          ILogger<AgentUpdateAvailabilityService> logger,
                                          TimeProvider? timeProvider = null)
    {
        this.versionService = versionService ?? throw new ArgumentNullException(nameof(versionService));
        this.releaseClient = releaseClient ?? throw new ArgumentNullException(nameof(releaseClient));
        this.preferencesStore = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<AgentUpdateAvailability> CheckAsync(Plugin plugin, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plugin);

        var now = this.timeProvider.GetUtcNow();
        if (this.cache.TryGetValue(plugin.Id, out var cached) && now - cached.Timestamp < this.Ttl)
        {
            return cached.Value;
        }

        var lazy = this.inFlight.GetOrAdd(
            plugin.Id,
            _ => new Lazy<Task<AgentUpdateAvailability>>(
                () => this.CheckAndCacheAsync(plugin),
                LazyThreadSafetyMode.ExecutionAndPublication));

        try
        {
            return await lazy.Value.WaitAsync(cancellationToken);
        }
        finally
        {
            if (lazy.IsValueCreated && lazy.Value.IsCompleted)
            {
                this.inFlight.TryRemove(plugin.Id, out _);
            }
        }
    }

    /// <inheritdoc />
    public void Invalidate() => this.cache.Clear();

    /// <inheritdoc />
    /// <remarks>
    ///   Only a completed cache entry is removed; the in-flight map is deliberately left alone so
    ///   concurrent callers keep sharing one check. A check that started before this call may
    ///   still publish its result, which is accepted as at most one check old.
    /// </remarks>
    public void InvalidateFor(Plugin plugin)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        this.cache.TryRemove(plugin.Id, out _);
    }

    private async Task<AgentUpdateAvailability> CheckAndCacheAsync(Plugin plugin)
    {
        var availability = await this.ComputeAvailabilityAsync(plugin).ConfigureAwait(false);
        this.cache[plugin.Id] = new CacheEntry(availability, this.timeProvider.GetUtcNow());
        return availability;
    }

    private async Task<AgentUpdateAvailability> ComputeAvailabilityAsync(Plugin plugin)
    {
        var updateCheck = plugin.UpdateCheck;
        if (updateCheck is null)
        {
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }

        if (!this.IsSupportedBlock(plugin, updateCheck))
        {
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }

        // The shared in-flight check runs to completion independently of any single caller;
        // each caller observes cancellation through WaitAsync in CheckAsync.
        var installedRaw = await this.versionService
            .GetVersionAsync(plugin, CancellationToken.None)
            .ConfigureAwait(false);
        if (!AgentUpdateVersion.TryExtract(installedRaw, out var installed))
        {
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }

        string? latestRaw;
        try
        {
            latestRaw = await this.releaseClient
                .GetLatestVersionAsync(updateCheck, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            this.logger.LogWarning("Update source request for {PluginId} timed out", plugin.Id);
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Update source request for {PluginId} failed", plugin.Id);
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }

        if (!AgentUpdateVersion.TryExtract(latestRaw, out var latest))
        {
            return new AgentUpdateAvailability(AgentUpdateStatus.Unknown);
        }

        var comparison = AgentUpdateVersion.Compare(installed, latest);
        if (comparison < 0)
        {
            return new AgentUpdateAvailability(
                AgentUpdateStatus.UpdateAvailable,
                installed.ToString(),
                latest.ToString());
        }

        return new AgentUpdateAvailability(
            AgentUpdateStatus.UpToDate,
            installed.ToString(),
            latest.ToString());
    }

    private bool IsSupportedBlock(Plugin plugin, PluginUpdateCheck updateCheck)
    {
        if (UpdateCheckSource.TryResolve(updateCheck, out _, out var problem))
        {
            return true;
        }

        switch (problem)
        {
            case UpdateCheckSource.Problem.MissingPackage:
                this.logger.LogWarning(
                    "Agent {PluginId} declares an npm update check without a package; ignoring it",
                    plugin.Id);
                break;

            case UpdateCheckSource.Problem.MissingRepo:
                this.logger.LogWarning(
                    "Agent {PluginId} declares a githubReleases update check without a repo; ignoring it",
                    plugin.Id);
                break;

            case UpdateCheckSource.Problem.UnknownSource:
                this.logger.LogWarning(
                    "Agent {PluginId} declares an unknown update check source {Source}; ignoring it",
                    plugin.Id,
                    updateCheck.Source);
                break;
        }

        return false;
    }

    private TimeSpan Ttl => AgentProbePolicy.ResolveTtl(this.preferencesStore.Load().AgentProbeTtlMinutes);

    private readonly record struct CacheEntry(AgentUpdateAvailability Value, DateTimeOffset Timestamp);
}
