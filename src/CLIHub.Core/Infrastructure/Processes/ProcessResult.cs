namespace CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Contains the result of a process execution with output capture.
/// </summary>
public sealed record ProcessResult
{
    /// <summary>
    ///   Gets the standard output captured from the process.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    ///   Gets the standard error output captured from the process.
    /// </summary>
    public required string StandardError { get; init; }

    /// <summary>
    ///   Gets the exit code of the process. Returns -1 if the process timed out.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    ///   Gets a value indicating whether the process exceeded the timeout and was killed.
    /// </summary>
    public required bool TimedOut { get; init; }
}
