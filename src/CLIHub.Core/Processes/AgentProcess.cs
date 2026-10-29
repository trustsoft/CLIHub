namespace CLIHub.Core.Processes;

/// <summary>
/// Represents a running agent process with its execution context.
/// </summary>
/// <remarks>
/// The <see cref="Runtime"/> property is null for runtime-hosted processes
/// (Node.js, Python) where the agent runs inside a shared runtime host.
/// </remarks>
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
    /// Gets the runtime executable for shell-hosted processes, or null for runtime-hosted processes.
    /// </summary>
    /// <remarks>
    /// This is null for processes hosted inside Node.js or Python runtimes.
    /// For shell-hosted processes, this identifies the terminal or interpreter.
    /// </remarks>
    public RuntimeExecutable? Runtime { get; init; }

    /// <summary>
    /// Gets when the process was started.
    /// </summary>
    public required DateTime StartTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether this is a runtime-hosted process (Node.js or Python).
    /// </summary>
    /// <remarks>
    /// Runtime-hosted processes have a null <see cref="Runtime"/> property.
    /// </remarks>
    public bool IsRuntimeHosted => this.Runtime is null;
}
