namespace CLIHub.Core.Updates;

/// <summary>
///   Provides the current application version.
/// </summary>
public interface IUpdateVersionProvider
{
    /// <summary>
    ///   Returns the current application version, or "unknown" when it cannot be determined.
    /// </summary>
    string GetCurrentVersion();
}
