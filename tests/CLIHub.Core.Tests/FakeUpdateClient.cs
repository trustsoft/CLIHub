using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Tests;

internal sealed class FakeUpdateClient : IUpdateClient
{
    public bool IsInstalled { get; set; } = true;

    public UpdateCheckResult CheckResult { get; set; } = UpdateCheckResult.None;

    public bool DownloadResult { get; set; } = true;

    public Exception? CheckException { get; set; }

    public int CheckCalls { get; private set; }

    public int DownloadCalls { get; private set; }

    public int ApplyCalls { get; private set; }

    public Task<UpdateCheckResult> CheckAsync()
    {
        CheckCalls++;
        if (CheckException is not null)
        {
            throw CheckException;
        }

        return Task.FromResult(CheckResult);
    }

    public Task<bool> DownloadAsync()
    {
        DownloadCalls++;
        return Task.FromResult(DownloadResult);
    }

    public void ApplyAndRestart() => ApplyCalls++;
}
