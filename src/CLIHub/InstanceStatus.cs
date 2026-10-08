namespace CLIHub;

/// <summary>
///   Result of single-instance coordination.
/// </summary>
public enum InstanceStatus
{
    /// <summary>
    ///   This is the first running instance of the application.
    /// </summary>
    FirstInstance,

    /// <summary>
    ///   Another instance is already running; this instance should exit.
    /// </summary>
    SecondInstance
}
