namespace CLIHub.Core.Processes;

using CLIHub.Core.Models;

/// <summary>
/// Represents an executable runtime host (terminal or interpreter).
/// </summary>
public record RuntimeExecutable
{
    /// <summary>
    /// Gets the runtime kind.
    /// </summary>
    public required RuntimeKind Kind { get; init; }

    /// <summary>
    /// Gets the full path to the runtime executable.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    /// Gets the runtime version, if available.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Creates a new <see cref="RuntimeExecutable"/> instance.
    /// </summary>
    /// <param name="kind">The runtime kind.</param>
    /// <param name="path">The full path to the runtime executable.</param>
    /// <param name="version">The runtime version, if available.</param>
    /// <returns>A new <see cref="RuntimeExecutable"/> instance.</returns>
    public static RuntimeExecutable Create(RuntimeKind kind, string path, string? version = null)
        => new() { Kind = kind, ExecutablePath = path, Version = version };
}
