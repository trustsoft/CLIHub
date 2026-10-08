namespace CLIHub;

/// <summary>
///   Coordinates application shell creation and event wiring.
/// </summary>
public interface IShellCoordinator : IDisposable
{
    /// <summary>
    ///   Gets the startup UI created and wired by this coordinator.
    /// </summary>
    IApplicationStartupUi StartupUi { get; }
}
