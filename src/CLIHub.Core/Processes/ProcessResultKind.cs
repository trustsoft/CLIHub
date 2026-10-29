namespace CLIHub.Core.Processes;

/// <summary>
/// Indicates the outcome of a process execution.
/// </summary>
public enum ProcessResultKind
{
    /// <summary>
    /// The process executed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// The process failed to start or terminated with an error.
    /// </summary>
    Failure,

    /// <summary>
    /// The process exceeded its timeout limit.
    /// </summary>
    Timeout,

    /// <summary>
    /// The process execution was cancelled.
    /// </summary>
    Cancelled
}
