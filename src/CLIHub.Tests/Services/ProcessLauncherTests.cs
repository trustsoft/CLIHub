using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace CLIHub.Tests.Services;

public class ProcessLauncherTests
{
    [Fact]
    public async Task CaptureOutputAsync_CapturesStandardOutput()
    {
        var launcher = new ProcessLauncher(NullLogger<ProcessLauncher>.Instance);

        var result = await launcher.CaptureOutputAsync("echo", "hello-capture", Path.GetTempPath());

        Assert.True(result.Started);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello-capture", result.StdOut);
    }

    [Fact]
    public async Task CaptureOutputAsync_MissingExecutable_ReportsFailure()
    {
        var launcher = new ProcessLauncher(NullLogger<ProcessLauncher>.Instance);

        var result = await launcher.CaptureOutputAsync(
            "this-command-does-not-exist-xyz", null, Path.GetTempPath());

        Assert.NotEqual(0, result.ExitCode);
    }
}
