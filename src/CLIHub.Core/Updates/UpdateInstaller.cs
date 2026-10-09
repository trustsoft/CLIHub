namespace CLIHub.Core.Updates;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

using Velopack;

/// <summary>
///   Handles update installation and restart logic.
/// </summary>
internal sealed class UpdateInstaller
{
    private readonly ILogger _logger;

    /// <summary>
    ///   Creates the update installer.
    /// </summary>
    /// <param name="logger"> Logger for installation operations. </param>
    public UpdateInstaller(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    ///   Applies a downloaded update and restarts the application.
    /// </summary>
    /// <param name="manager"> The UpdateManager to use for installation. </param>
    /// <param name="asset"> The downloaded asset to install. </param>
    /// <returns> The installation result. </returns>
    public UpdateInstallResult ApplyAndRestart(UpdateManager manager, VelopackAsset? asset)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (asset is null)
        {
            _logger.LogWarning("No downloaded update to install");
            return new UpdateInstallResult(UpdateInstallStatus.NoDownload);
        }

        var version = Normalize(asset.Version.ToString());

        try
        {
            _logger.LogInformation("Applying update and restarting: {Version}", version);
            
            // This method does not return if successful - it terminates the current process
            manager.ApplyUpdatesAndRestart(asset);

            // If we reach here, restart failed
            _logger.LogWarning("Update restart failed for version {Version}", version);
            return new UpdateInstallResult(UpdateInstallStatus.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply update {Version}", version);
            return new UpdateInstallResult(UpdateInstallStatus.Failed);
        }
    }

    /// <summary>
    ///   Applies a downloaded update and exits the application without restart.
    /// </summary>
    /// <param name="manager"> The UpdateManager to use for installation. </param>
    /// <param name="asset"> The downloaded asset to install. </param>
    /// <returns> The installation result. </returns>
    public UpdateInstallResult ApplyAndExit(UpdateManager manager, VelopackAsset? asset)
    {
        ArgumentNullException.ThrowIfNull(manager);

        if (asset is null)
        {
            _logger.LogWarning("No downloaded update to install");
            return new UpdateInstallResult(UpdateInstallStatus.NoDownload);
        }

        var version = Normalize(asset.Version.ToString());

        try
        {
            _logger.LogInformation("Applying update and exiting: {Version}", version);
            
            // This method does not return if successful - it terminates the current process
            manager.ApplyUpdatesAndExit(asset);

            // If we reach here, exit failed
            _logger.LogWarning("Update exit failed for version {Version}", version);
            return new UpdateInstallResult(UpdateInstallStatus.Failed);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply update {Version}", version);
            return new UpdateInstallResult(UpdateInstallStatus.Failed);
        }
    }

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
