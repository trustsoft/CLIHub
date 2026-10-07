namespace CLIHub;

/// <summary>
///   Coordinates the first-instance application startup sequence.
/// </summary>
public interface IApplicationBootstrapper
{
    /// <summary>
    ///   Runs startup after WPF has prepared the application environment.
    /// </summary>
    /// <param name="context"> WPF callbacks required by startup orchestration. </param>
    void Start(ApplicationStartupContext context);
}
