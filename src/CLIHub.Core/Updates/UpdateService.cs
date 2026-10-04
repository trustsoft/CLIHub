namespace CLIHub.Core.Services;

using System.Reflection;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Interfaces;
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

    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager? _injectedManager;
    private readonly Lazy<UpdateManager?> _defaultManager;
    private readonly object _downloadGate = new();
    private bool _isDownloading;
    private VelopackAsset? _downloadedAsset;
    private string? _lastKnownAvailableVersion;

    /// <summary>
    ///   Creates the service using the default GitHub update source.
    /// </summary>
    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _defaultManager = new Lazy<UpdateManager?>(CreateDefaultManager);
    }

    /// <summary>
    ///   Test seam: uses a caller-provided manager (for example one built with a
    ///   <c>TestVelopackLocator</c>).
    /// </summary>
    internal UpdateService(ILogger<UpdateService> logger, UpdateManager manager)
    {
        _logger = logger;
        _injectedManager = manager;
        _defaultManager = new Lazy<UpdateManager?>(() => manager);
    }

    private UpdateManager? Manager => _injectedManager ?? _defaultManager.Value;

    private UpdateManager? CreateDefaultManager()
    {
        try
        {
            // A GitHub repository URL uses the GitHub Releases source; any other URL or a local
            // folder (useful for testing and self-hosted mirrors) uses Velopack's default source.
            if (RepositoryUrl.Contains("github.com", StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateManager(new GithubSource(RepositoryUrl, null, false));
            }

            return new UpdateManager(RepositoryUrl);
        }
        catch (Exception ex)
        {
            // Thrown when VelopackApp.Build().Run() has not initialized a locator
            // (for example, running under a test host).
            _logger.LogDebug(ex, "Velopack is not initialized; update checks are disabled");
            return null;
        }
    }

    /// <inheritdoc />
    public string GetCurrentVersion()
    {
        if (Manager?.CurrentVersion is { } version)
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
        var (result, _) = await CheckForUpdateCoreAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public bool IsDownloading
    {
        get
        {
            lock (_downloadGate)
            {
                return _isDownloading;
            }
        }
    }

    /// <inheritdoc />
    public string? LastKnownAvailableVersion => _lastKnownAvailableVersion;

    /// <inheritdoc />
    public event EventHandler? UpdateStateChanged;

    /// <inheritdoc />
    public async Task<UpdateDownloadResult> DownloadUpdateAsync(CancellationToken cancellationToken = default)
    {
        lock (_downloadGate)
        {
            if (_isDownloading)
            {
                _logger.LogInformation("Update download requested while another download is running");
                return new UpdateDownloadResult(UpdateDownloadStatus.AlreadyDownloading, null);
            }

            _isDownloading = true;
        }

        try
        {
            var (result, update) = await CheckForUpdateCoreAsync(cancellationToken);

            if (result.Status != UpdateStatus.UpdateAvailable || update?.TargetFullRelease is not { } asset)
            {
                var status = result.Status switch
                {
                    UpdateStatus.NotInstalled => UpdateDownloadStatus.NotInstalled,
                    UpdateStatus.UpToDate => UpdateDownloadStatus.NoUpdate,
                    _ => UpdateDownloadStatus.Failed,
                };

                _logger.LogInformation("Update download not started: {Status}", status);
                return new UpdateDownloadResult(status, null);
            }

            try
            {
                await Manager!.DownloadUpdatesAsync(update, null, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Update download cancelled");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, result.AvailableVersion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Update download failed");
                return new UpdateDownloadResult(UpdateDownloadStatus.Failed, result.AvailableVersion);
            }

            lock (_downloadGate)
            {
                _downloadedAsset = asset;
            }

            var version = result.AvailableVersion;
            _logger.LogInformation("Update downloaded: {Version}", version);
            return new UpdateDownloadResult(UpdateDownloadStatus.Downloaded, version);
        }
        finally
        {
            lock (_downloadGate)
            {
                _isDownloading = false;
            }

            RaiseStateChanged();
        }
    }

    /// <inheritdoc />
    public void ApplyDownloadedUpdateAndRestart()
    {
        var manager = Manager;
        VelopackAsset? asset;

        lock (_downloadGate)
        {
            asset = _downloadedAsset;
        }

        if (manager is null || asset is null)
        {
            _logger.LogInformation("No downloaded update to apply; restart skipped");
            return;
        }

        try
        {
            manager.ApplyUpdatesAndRestart(asset);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply the downloaded update");
            throw;
        }
    }

    private async Task<(UpdateCheckResult Result, UpdateInfo? Update)> CheckForUpdateCoreAsync(
        CancellationToken cancellationToken)
    {
        var current = GetCurrentVersion();
        var manager = Manager;

        if (manager?.CurrentVersion == null)
        {
            _logger.LogInformation("Update check skipped: the application is not a Velopack install");
            SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.NotInstalled, current, null), null);
        }

        try
        {
            var checkTask = manager.CheckForUpdatesAsync();
            var completed = await Task.WhenAny(checkTask, Task.Delay(CheckTimeout, cancellationToken));

            if (completed != checkTask)
            {
                _logger.LogWarning("Update check timed out after {Seconds}s", CheckTimeout.TotalSeconds);
                SetAvailableVersion(null);
                return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
            }

            var update = await checkTask;
            if (update?.TargetFullRelease?.Version is { } version)
            {
                var available = Normalize(version.ToString());
                _logger.LogInformation("Update available: {Version}", available);
                SetAvailableVersion(available);
                return (new UpdateCheckResult(UpdateStatus.UpdateAvailable, current, available), update);
            }

            _logger.LogInformation("No update available");
            SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.UpToDate, current, null), null);
        }
        catch (NotInstalledException)
        {
            _logger.LogInformation("Update check skipped: the application is not a Velopack install");
            SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.NotInstalled, current, null), null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check cancelled");
            SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            SetAvailableVersion(null);
            return (new UpdateCheckResult(UpdateStatus.Failed, current, null), null);
        }
    }

    private void SetAvailableVersion(string? version)
    {
        var changed = !string.Equals(Interlocked.Exchange(ref _lastKnownAvailableVersion, version), version, StringComparison.Ordinal);

        if (changed)
        {
            RaiseStateChanged();
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
