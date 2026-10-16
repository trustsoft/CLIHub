namespace CLIHub.Core.Updates;

/// <summary>
///   Checks for application updates and reports the current version.
/// </summary>
public interface IUpdateService : IUpdateVersionProvider,
                                  IUpdateChecker,
                                  IUpdateStateSource,
                                  IUpdateDownloader,
                                  IUpdateInstaller
{
}
