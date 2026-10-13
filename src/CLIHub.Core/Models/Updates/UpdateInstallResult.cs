namespace CLIHub.Core.Models;

/// <summary>
///   Outcome of an update installation request.
/// </summary>
public enum UpdateInstallStatus
{
    /// <summary>
    ///   The update was applied and the application will restart or exit.
    /// </summary>
    Applied,

    /// <summary>
    ///   No downloaded update is available to install.
    /// </summary>
    NoDownload,

    /// <summary>
    ///   The installation failed.
    /// </summary>
    Failed
}

/// <summary>
///   Result of an update installation request.
/// </summary>
/// <param name="Status"> The installation outcome. </param>
public sealed record UpdateInstallResult(UpdateInstallStatus Status);
