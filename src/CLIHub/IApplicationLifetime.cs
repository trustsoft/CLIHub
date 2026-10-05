namespace CLIHub;

/// <summary>
///   Controls the lifetime of the running application.
/// </summary>
public interface IApplicationLifetime
{
    /// <summary>
    ///   Requests application shutdown.
    /// </summary>
    void Shutdown();
}
