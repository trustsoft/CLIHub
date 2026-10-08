namespace CLIHub;

/// <summary>
///   Coordinates single-instance detection and activation signaling.
/// </summary>
public interface IInstanceCoordinator
{
    /// <summary>
    ///   Checks whether this is the first instance and coordinates activation or shutdown.
    /// </summary>
    /// <returns>
    ///   <see cref="InstanceStatus.FirstInstance"/> if this is the first instance and startup
    ///   should continue, or <see cref="InstanceStatus.SecondInstance"/> if another instance
    ///   is already running and this instance should exit.
    /// </returns>
    InstanceStatus Coordinate();
}
