namespace CLIHub.Core.Interfaces;

/// <summary>
///   Manages the per-user Windows Run registration that starts the application with Windows.
/// </summary>
public interface IStartupService
{
    /// <summary>
    ///   True when a per-user startup registration for the application currently exists.
    /// </summary>
    bool IsEnabled();

    /// <summary>
    ///   Creates (enabled) or removes (disabled) the per-user startup registration.
    ///   Never throws; returns false when the registration could not be updated.
    /// </summary>
    bool SetEnabled(bool enabled);
}
