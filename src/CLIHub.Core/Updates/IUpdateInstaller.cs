namespace CLIHub.Core.Updates;

/// <summary>
///   Applies a downloaded update and restarts the application.
/// </summary>
public interface IUpdateInstaller
{
    /// <summary>
    ///   Applies the downloaded update and restarts the application.
    /// </summary>
    void ApplyDownloadedUpdateAndRestart();
}
