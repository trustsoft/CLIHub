using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Tests;

internal sealed class FakeUpdateClient : IUpdateClient
{
    public bool IsInstalled { get; set; } = true;

    public UpdateCheckResult CheckResult { get; set; } = UpdateCheckResult.None;

    public bool DownloadResult { get; set; } = true;

    public Exception? CheckException { get; set; }

    public TaskCompletionSource? CheckGate { get; set; }

    public int CheckCalls { get; private set; }

    public int DownloadCalls { get; private set; }

    public int ApplyCalls { get; private set; }

    public async Task<UpdateCheckResult> CheckAsync()
    {
        CheckCalls++;
        if (CheckGate is not null)
        {
            await CheckGate.Task;
        }

        if (CheckException is not null)
        {
            throw CheckException;
        }

        return CheckResult;
    }

    public Task<bool> DownloadAsync()
    {
        DownloadCalls++;
        return Task.FromResult(DownloadResult);
    }

    public void ApplyAndRestart() => ApplyCalls++;
}
