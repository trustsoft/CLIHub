namespace CLIHub.Core.Configuration;

/// <summary>
///   Provides access to the application configuration.
/// </summary>
public interface IConfigService : IDisposable
{
    /// <summary>
    ///   Loads the configuration from disk.
    /// </summary>
    /// <returns> The loaded configuration, or the default configuration when the file does not exist. </returns>
    ConfigurationSnapshot Load();

    /// <summary>
    ///   Schedules the configuration for prompt asynchronous persistence; the calling thread is
    ///   not blocked by disk I/O.
    /// </summary>
    /// <param name="snapshot"> The detached configuration snapshot to save. </param>
    void Save(ConfigurationSnapshot snapshot);

    /// <summary>
    ///   Writes all pending configuration changes to disk synchronously, so the stored state is
    ///   guaranteed to be current when this call returns.
    /// </summary>
    void Flush();
}
