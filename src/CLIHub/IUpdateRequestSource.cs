namespace CLIHub;

/// <summary>
///   Exposes the update download request raised by a presentation surface.
/// </summary>
public interface IUpdateRequestSource
{
    /// <summary>
    ///   Raised when the presentation surface requests an update download.
    /// </summary>
    event EventHandler? UpdateRequested;
}
