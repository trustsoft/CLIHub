namespace CLIHub;

using CLIHub.Core.Infrastructure.Windows;

/// <summary>
///   Coordinates single-instance detection and activation signaling.
/// </summary>
public sealed class InstanceCoordinator : IInstanceCoordinator
{
    private readonly ISingleInstanceGuard _singleInstanceGuard;
    private readonly IApplicationLifetime _applicationLifetime;

    /// <summary>
    ///   Creates the instance coordinator.
    /// </summary>
    /// <param name="singleInstanceGuard"> Single-instance boundary. </param>
    /// <param name="applicationLifetime"> Application shutdown boundary. </param>
    public InstanceCoordinator(
        ISingleInstanceGuard singleInstanceGuard,
        IApplicationLifetime applicationLifetime)
    {
        _singleInstanceGuard = singleInstanceGuard ?? throw new ArgumentNullException(nameof(singleInstanceGuard));
        _applicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
    }

    /// <inheritdoc />
    public InstanceStatus Coordinate()
    {
        if (!_singleInstanceGuard.IsFirstInstance)
        {
            _singleInstanceGuard.SignalActivation();
            _applicationLifetime.Shutdown();
            return InstanceStatus.SecondInstance;
        }

        return InstanceStatus.FirstInstance;
    }
}
