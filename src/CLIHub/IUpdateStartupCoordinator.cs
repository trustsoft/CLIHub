namespace CLIHub;

/// <summary>
///   Coordinates the optional update check performed during application startup.
/// </summary>
public interface IUpdateStartupCoordinator
{
    /// <summary>
    ///   Checks for an update when enabled and reports an available version through the callback.
    /// </summary>
    /// <param name="enabled"> Whether startup update checks are enabled. </param>
    /// <param name="notifyUpdateAvailable"> Callback used to notify the UI of an available version. </param>
    /// <param name="cancellationToken"> Token used to cancel the startup check. </param>
    /// <returns> A task that completes when the startup check finishes. </returns>
    Task CheckAsync(
        bool enabled,
        Action<string> notifyUpdateAvailable,
        CancellationToken cancellationToken = default);
}
