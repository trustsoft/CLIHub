namespace CLIHub.Core.Plugins;

/// <summary>
///   Refreshes the plugin feed in the background on a fixed interval and publishes a
///   <see cref="Events.PluginFeedUpdatedEvent" /> when the feed's plugin tuples change.
/// </summary>
public interface IPluginFeedSyncService
{
    /// <summary>
    ///   Begins the background refresh loop: one refresh immediately, then one per interval. Does
    ///   not block the caller and never throws; a second call while running is a no-op, and a call
    ///   after disposal does nothing.
    /// </summary>
    void Start();

    /// <summary>
    ///   Cancels the pending refresh and the interval wait and waits at most
    ///   <paramref name="timeout" /> for an in-flight refresh to finish. Safe to call more than once.
    /// </summary>
    /// <param name="timeout"> The maximum time to wait for the in-flight refresh. </param>
    /// <returns> A task completing when the loop has stopped or the timeout elapsed. </returns>
    Task StopAsync(TimeSpan timeout);
}
