namespace CLIHub.Core.Updates;

/// <summary>
///   Exposes observable update state without update operations.
/// </summary>
public interface IUpdateStateSource
{
    /// <summary>
    ///   Gets a value indicating whether an update download is currently running.
    /// </summary>
    bool IsDownloading { get; }

    /// <summary>
    ///   Gets the most recently available update version, if any.
    /// </summary>
    string? LastKnownAvailableVersion { get; }

    /// <summary>
    ///   Raised when update availability or download state changes.
    /// </summary>
    event EventHandler? UpdateStateChanged;
}
