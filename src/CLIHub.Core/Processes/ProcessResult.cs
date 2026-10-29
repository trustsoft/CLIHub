namespace CLIHub.Core.Processes;

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public record ProcessResult
{
    /// <summary>
    /// Gets the outcome kind.
    /// </summary>
    public required ProcessResultKind Kind { get; init; }

    /// <summary>
    /// Gets the process exit code, if available.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets the captured standard output, if available.
    /// </summary>
    public string? Output { get; init; }

    /// <summary>
    /// Gets the error message or captured standard error, if available.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets the execution duration, if measured.
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Gets a value indicating whether the process executed successfully.
    /// </summary>
    public bool IsSuccess => this.Kind == ProcessResultKind.Success;

    /// <summary>
    /// Creates a success result.
    /// </summary>
    /// <param name="exitCode">The process exit code.</param>
    /// <param name="output">The captured standard output.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>A success <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Success(int exitCode, string? output = null, TimeSpan? duration = null)
        => new()
        {
            Kind = ProcessResultKind.Success,
            ExitCode = exitCode,
            Output = output,
            Duration = duration
        };

    /// <summary>
    /// Creates a failure result.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A failure <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Failure(string error)
        => new() { Kind = ProcessResultKind.Failure, Error = error };

    /// <summary>
    /// Creates a timeout result.
    /// </summary>
    /// <returns>A timeout <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Timeout()
        => new() { Kind = ProcessResultKind.Timeout };

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    /// <returns>A cancelled <see cref="ProcessResult"/>.</returns>
    public static ProcessResult Cancelled()
        => new() { Kind = ProcessResultKind.Cancelled };
}
