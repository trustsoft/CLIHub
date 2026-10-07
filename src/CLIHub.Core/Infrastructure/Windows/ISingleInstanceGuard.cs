namespace CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Provides the application-facing single-instance boundary.
/// </summary>
public interface ISingleInstanceGuard
{
    /// <summary>
    ///   Gets a value indicating whether this process acquired the first-instance ownership.
    /// </summary>
    bool IsFirstInstance { get; }

    /// <summary>
    ///   Raised when another instance requests activation of this process.
    /// </summary>
    event Action? ActivationRequested;

    /// <summary>
    ///   Signals the first instance from a second-instance process.
    /// </summary>
    void SignalActivation();
}
