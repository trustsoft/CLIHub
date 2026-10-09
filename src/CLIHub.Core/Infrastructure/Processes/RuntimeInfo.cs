namespace CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Contains information about the detected or selected command-line runtime.
/// </summary>
public sealed record RuntimeInfo
{
    /// <summary>
    ///   Gets the full path or executable name of the runtime.
    /// </summary>
    public required string ExecutablePath { get; init; }

    /// <summary>
    ///   Gets the type of runtime.
    /// </summary>
    public required RuntimeType Type { get; init; }
}
