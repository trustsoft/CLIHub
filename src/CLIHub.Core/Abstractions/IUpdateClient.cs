using CLIHub.Core.Models;

namespace CLIHub.Core.Abstractions;

public interface IUpdateClient
{
    bool IsInstalled { get; }

    Task<UpdateCheckResult> CheckAsync();

    Task<bool> DownloadAsync();

    void ApplyAndRestart();
}
