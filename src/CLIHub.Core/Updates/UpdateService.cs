namespace CLIHub.Core.Updates;

using System.Reflection;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Configuration;
using CLIHub.Core.Models;

using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

/// <summary>
///   Checks for, downloads, and applies updates via Velopack and reports the current version.
/// </summary>
public class UpdateService : IUpdateService
{
    // The release repository used as the Velopack update feed (GitHub Releases).
    // A non-GitHub URL or local folder is also supported (see CreateDefaultManager).
    internal const string RepositoryUrl = "https://github.com/trustsoft/clihub";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    private readonly ILogger<UpdateService> logger;
    private readonly IPreferencesStore? preferences;
    private readonly UpdateManager? injectedManager;
    private readonly TimeSpan checkTimeout;
    private readonly object managerGate = new();
    private UpdateManager? cachedManager;
    private bool cachedManagerPrerelease;
    private bool managerCached;
    private readonly UpdateChecker checker;
    private readonly UpdateDownloader downloader;
    private readonly UpdateInstaller installer;
    private readonly object downloadGate = new();
    private bool isDownloadInProgress;
    private string? lastKnownAvailableVersion;

    /// <summary>
    ///   Creates the service using the default GitHub update source.
    /// </summary>
    /// <param name="logger"> The service logger. </param>
    /// <param name="preferencesStore"> Preferences source used to decide pre-release checks. </param>
    public UpdateService(ILogger<UpdateService> logger, IPreferencesStore preferencesStore)
    {
        this.logger = logger;
        this.preferences = preferencesStore ?? throw new ArgumentNullException(nameof(preferencesStore));
        this.checkTimeout = CheckTimeout;
        this.checker = new UpdateChecker(logger);
        this.downloader = new UpdateDownloader(logger);
        this.installer = new UpdateInstaller(logger);
    }

    /// <summary>
    ///   Test seam: uses a caller-provided manager (for example one built with a
    ///   <c>TestVelopackLocator</c>).
    /// </summary>
    private UpdateService(ILogger<UpdateService> logger, UpdateManager manager, TimeSpan? checkTimeout)
    {
        this.logger = logger;
        this.checkTimeout = checkTimeout ?? CheckTimeout;
        this.injectedManager = manager;
        this.checker = new UpdateChecker(logger);
        this.downloader = new UpdateDownloader(logger);
        this.installer = new UpdateInstaller(logger);
    }

    /// <summary>
    ///   Creates an update service with an injected manager for tests.
    /// </summary>
    /// <param name="logger"> The service logger. </param>
    /// <param name="manager"> The test update manager. </param>
    /// <param name="checkTimeout"> Optional timeout used by the test. </param>
    internal static UpdateService CreateForTesting(
        ILogger<UpdateService> logger,
        UpdateManager manager,
        TimeSpan? checkTimeout = null) => new(logger, manager, checkTimeout);

    private UpdateManager? Manager => this.injectedManager ?? this.GetDefaultManager();

    private UpdateManager? GetDefaultManager()
    {
        var prerelease = this.preferences?.Load().NotifyPrereleaseUpdates ?? false;

        lock (this.managerGate)
        {
            if (this.managerCached && this.cachedManagerPrerelease == prerelease)
            {
                return this.cachedManager;
            }

            this.cachedManager = this.CreateDefaultManager(prerelease);
            this.cachedManagerPrerelease = prerelease;
            this.managerCached = true;
            return this.cachedManager;
        }
    }

    private UpdateManager? CreateDefaultManager(bool prerelease)
    {
        try
        {
            // A GitHub repository URL uses the GitHub Releases source; any other URL or a local
            // folder (useful for testing and self-hosted mirrors) uses Velopack's default source.
            if (RepositoryUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateManager(new GithubSource(RepositoryUrl, null, prerelease));
            }

            return new UpdateManager(RepositoryUrl);
        }
        catch (Exception ex)
        {
            // Thrown when VelopackApp.Build().Run() has not initialized a locator
            // (for example, running under a test host).
            this.logger.LogDebug(ex, "Velopack is not initialized; update checks are disabled");
            return null;
        }
    }

    /// <inheritdoc />
    public string GetCurrentVersion()
    {
        if (this.Manager?.CurrentVersion is { } version)
        {
            return Normalize(version.ToString());
        }

        var informational = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return string.IsNullOrWhiteSpace(informational) ? "unknown" : Normalize(informational);
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var (result, _) = await this.CheckForUpdateCoreAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public bool IsDownloading
    {
        get
        {
            lock (this.downloadGate)
            {
                return this.isDownloadInProgress;
            }
        }
    }

    /// <inheritdoc />
    public string? LastKnownAvailableVersion => this.lastKnownAvailableVersion;

    /// <inheritdoc />
    public event EventHandler? UpdateStateChanged;

    /// <inheritdoc />
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        lock (this.downloadGate)
        {
            if (this.isDownloadInProgress)
            {
                this.logger.LogInformation("Update download requested while another download is running");
                return new UpdateDownloadResult(UpdateDownloadStatus.AlreadyDownloading, null);
            }

            this.isDownloadInProgress = true;
        }

        try
        {
            var (result, update) = await this.CheckForUpdateCoreAsync(cancellationToken);

            if (result.Status != UpdateStatus.UpdateAvailable || update is null)
            {
                var status = result.Status switch
                {
                    UpdateStatus.NotInstalled => UpdateDownloadStatus.NotInstalled,
                    UpdateStatus.UpToDate => UpdateDownloadStatus.NoUpdate,
                    _ => UpdateDownloadStatus.Failed,
                };

                this.logger.LogInformation("Update download not started: {Status}", status);
                return new UpdateDownloadResult(status, null);
            }

            var downloadResult = await this.downloader.DownloadAsync(this.Manager!, update, cancellationToken);

            return downloadResult;
        }
        finally
        {
            lock (this.downloadGate)
            {
                this.isDownloadInProgress = false;
            }

            this.RaiseStateChanged();
        }
    }

    /// <inheritdoc />
    public void ApplyDownloadedUpdateAndRestart()
    {
        var manager = this.Manager;
        var asset = this.downloader.DownloadedAsset;

        if (manager is null || asset is null)
        {
            this.logger.LogInformation("No downloaded update to apply; restart skipped");
            return;
        }

        try
        {
            var result = this.installer.ApplyAndRestart(manager, asset);
            
            if (result.Status == UpdateInstallStatus.Failed)
            {
                throw new InvalidOperationException("Failed to apply the downloaded update");
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to apply the downloaded update");
            throw;
        }
    }

    private async Task<(UpdateCheckResult Result, UpdateInfo? Update)> CheckForUpdateCoreAsync(
        CancellationToken cancellationToken)
    {
        var current = this.GetCurrentVersion();
        var manager = this.Manager;

        if (manager?.CurrentVersion == null)
        {
            this.logger.LogInformation("Update check skipped: the application is not a Velopack install");
            this.SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.NotInstalled, current, null), null);
        }

        try
        {
            var checkTask = manager.CheckForUpdatesAsync();
            var timeoutTask = Task.Delay(this.checkTimeout, cancellationToken);
            var completed = await Task.WhenAny(checkTask, timeoutTask);

            if (completed != checkTask)
            {
                _ = this.ObserveLateUpdateCheckAsync(checkTask);
                this.logger.LogWarning("Update check timed out after {Seconds}s", this.checkTimeout.TotalSeconds);
                this.SetAvailableVersion(null);
                return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
            }

            var update = await checkTask;
            if (update?.TargetFullRelease?.Version is { } version)
            {
                var available = Normalize(version.ToString());
                this.logger.LogInformation("Update available: {Version}", available);
                this.SetAvailableVersion(available);
                return (new UpdateCheckResult(UpdateStatus.UpdateAvailable, current, available), update);
            }

            this.logger.LogInformation("No update available");
            this.SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.UpToDate, current, null), null);
        }
        catch (NotInstalledException)
        {
            this.logger.LogInformation("Update check skipped: the application is not a Velopack install");
            this.SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.NotInstalled, current, null), null);
        }
        catch (OperationCanceledException)
        {
            this.logger.LogWarning("Update check cancelled");
            this.SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Update check failed");
            this.SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
        }
    }

    private async Task ObserveLateUpdateCheckAsync(Task<UpdateInfo?> checkTask)
    {
        try
        {
            await checkTask.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogDebug(ex, "Late update check completed after timeout");
        }
    }

    private void SetAvailableVersion(string? version)
    {
        var changed = !string.Equals(Interlocked.Exchange(ref this.lastKnownAvailableVersion, version), version, StringComparison.Ordinal);

        if (changed)
        {
            this.RaiseStateChanged();
        }
    }

    private void RaiseStateChanged() => UpdateStateChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    ///   Strips build metadata (for example, the "+commit" suffix) so the version
    ///   displays as a plain semantic version.
    /// </summary>
    private static string Normalize(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
