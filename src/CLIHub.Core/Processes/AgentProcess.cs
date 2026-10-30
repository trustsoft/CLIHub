namespace CLIHub.Core.Processes;

/// <summary>
/// Represents a running agent process with its execution context.
/// </summary>
public record AgentProcess
{
    /// <summary>
    /// Gets the operating system process identifier.
    /// </summary>
    public required int ProcessId { get; init; }

    /// <summary>
    /// Gets the full command line of the process.
    /// </summary>
    public required string CommandLine { get; init; }

    /// <summary>
    /// Gets the full path to the process executable.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Gets when the process was started.
    /// </summary>
    public required DateTime StartTime { get; init; }

}
