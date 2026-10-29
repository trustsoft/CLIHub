namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// Request to launch an agent command in a specific execution context.
/// </summary>
public record LaunchRequest
{
    /// <summary>
    /// Gets the working directory for the process.
    /// </summary>
    public required string WorkingDirectory { get; init; }

    /// <summary>
    /// Gets the command to execute.
    /// </summary>
    public required PluginCommand Command { get; init; }

    /// <summary>
    /// Gets the runtime kind to use for execution.
    /// </summary>
    public required RuntimeKind RuntimeKind { get; init; }

    /// <summary>
    /// Gets the cancellation token for the operation.
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Creates a new <see cref="LaunchRequest"/> instance.
    /// </summary>
    /// <param name="workingDirectory">The working directory for the process.</param>
    /// <param name="command">The command to execute.</param>
    /// <param name="runtimeKind">The runtime kind to use for execution.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A new <see cref="LaunchRequest"/> instance.</returns>
    public static LaunchRequest Create(
        string workingDirectory,
        PluginCommand command,
        RuntimeKind runtimeKind,
        CancellationToken cancellationToken = default)
        => new()
        {
            WorkingDirectory = workingDirectory,
            Command = command,
            RuntimeKind = runtimeKind,
            CancellationToken = cancellationToken
        };
}
