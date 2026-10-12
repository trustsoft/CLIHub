namespace CLIHub;

using CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Coordinates single-instance detection and activation signaling.
/// </summary>
public sealed class InstanceCoordinator : IInstanceCoordinator
{
    private readonly ISingleInstanceGuard singleInstanceGuard;
    private readonly IApplicationLifetime applicationLifetime;

    /// <summary>
    ///   Creates the instance coordinator.
    /// </summary>
    /// <param name="singleInstanceGuard"> Single-instance boundary. </param>
    /// <param name="applicationLifetime"> Application shutdown boundary. </param>
    public InstanceCoordinator(
        ISingleInstanceGuard singleInstanceGuard,
        IApplicationLifetime applicationLifetime)
    {
        this.singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        this.applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    /// <inheritdoc />
    public InstanceStatus Coordinate()
    {
        if (!this.singleInstanceGuard.IsFirstInstance)
        {
            this.singleInstanceGuard.SignalActivation();
            this.applicationLifetime.Shutdown();
            return InstanceStatus.SecondInstance;
        }

        return InstanceStatus.FirstInstance;
    }
}
