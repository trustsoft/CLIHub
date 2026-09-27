using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Velopack;
using Velopack.Exceptions;
using Velopack.Sources;

namespace CLIHub.Core.Services;

/// <summary>
/// Checks for updates via Velopack and reports the current version.
/// </summary>
public class UpdateService : IUpdateService
{
    // Set to the release repository when packaging with Velopack (GitHub Releases).
    internal const string RepositoryUrl = "https://github.com/your-org/clihub";

    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(15);

    private readonly ILogger<UpdateService> _logger;
    private readonly UpdateManager? _injectedManager;
    private readonly Lazy<UpdateManager?> _defaultManager;

    /// <summary>
    /// Creates the service using the default GitHub update source.
    /// </summary>
    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
        _defaultManager = new Lazy<UpdateManager?>(CreateDefaultManager);
    }

    /// <summary>
    /// Test seam: uses a caller-provided manager (for example one built with a
    /// <c>TestVelopackLocator</c>).
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
            return new UpdateManager(new GithubSource(RepositoryUrl, null, false));
        }
        catch (Exception ex)
        {
            // Thrown when VelopackApp.Build().Run() has not initialized a locator
            // (for example, running under a test host).
            _logger.LogDebug(ex, "Velopack is not initialized; update checks are disabled");
            return null;
        }
    }

    public string GetCurrentVersion()
    {
        if (Manager?.CurrentVersion is { } version)
            return Normalize(version.ToString());

        var informational = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return string.IsNullOrWhiteSpace(informational) ? "unknown" : Normalize(informational);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = GetCurrentVersion();
        var manager = Manager;

        if (manager?.CurrentVersion == null)
        {
            _logger.LogInformation("Update check skipped: the application is not a Velopack install");
            return new UpdateCheckResult(UpdateStatus.NotInstalled, current, null);
        }

        try
        {
            var checkTask = manager.CheckForUpdatesAsync();
            var completed = await Task.WhenAny(checkTask, Task.Delay(CheckTimeout, cancellationToken));

            if (completed != checkTask)
            {
                _logger.LogWarning("Update check timed out after {Seconds}s", CheckTimeout.TotalSeconds);
                return new UpdateCheckResult(UpdateStatus.Failed, current, null);
            }

            var update = await checkTask;
            if (update?.TargetFullRelease?.Version is { } version)
            {
                var available = Normalize(version.ToString());
                _logger.LogInformation("Update available: {Version}", available);
                return new UpdateCheckResult(UpdateStatus.UpdateAvailable, current, available);
            }

            _logger.LogInformation("No update available");
            return new UpdateCheckResult(UpdateStatus.UpToDate, current, null);
        }
        catch (NotInstalledException)
        {
            _logger.LogInformation("Update check skipped: the application is not a Velopack install");
            return new UpdateCheckResult(UpdateStatus.NotInstalled, current, null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Update check cancelled");
            return new UpdateCheckResult(UpdateStatus.Failed, current, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Update check failed");
            return new UpdateCheckResult(UpdateStatus.Failed, current, null);
        }
    }

    /// <summary>
    /// Strips build metadata (for example, the "+commit" suffix) so the version
    /// displays as a plain semantic version.
    /// </summary>
    private static string Normalize(string version)
    {
        var plus = version.IndexOf('+');
        return plus >= 0 ? version[..plus] : version;
    }
}
