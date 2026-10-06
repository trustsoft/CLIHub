namespace CLIHub.Core.Configuration;

/// <summary>
///   Owns application configuration snapshots and their persistence lifecycle.
/// </summary>
public interface IConfigurationRepository : IDisposable
{
    /// <summary>
    ///   Reads the current detached configuration snapshot.
    /// </summary>
    /// <returns> A detached snapshot of the current configuration. </returns>
    ConfigurationSnapshot Read();

    /// <summary>
    ///   Applies a synchronous update to the latest snapshot and schedules it for persistence after
    ///   the callback completes successfully.
    /// </summary>
    /// <param name="update"> The update applied to a detached snapshot. </param>
    void Update(Action<ConfigurationSnapshot> update);

    /// <summary>
    ///   Writes completed configuration updates to disk synchronously before returning.
    /// </summary>
    void Flush();
}
