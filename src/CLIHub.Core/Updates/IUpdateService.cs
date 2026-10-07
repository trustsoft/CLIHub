namespace CLIHub.Core.Updates;

using CLIHub.Core.Models;

/// <summary>
///   Checks for application updates and reports the current version.
/// </summary>
public interface IUpdateService :
    IUpdateVersionProvider,
    IUpdateChecker,
    IUpdateStateSource,
    IUpdateDownloader,
    IUpdateInstaller
{
}
