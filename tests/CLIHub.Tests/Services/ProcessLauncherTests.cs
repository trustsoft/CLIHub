namespace CLIHub.Tests.Services;

using CLIHub.Core.Models;
using CLIHub.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

public class ProcessLauncherTests
{
    [Theory]
    [InlineData(RuntimeKind.WindowsTerminal, "wt.exe")]
    [InlineData(RuntimeKind.CommandPrompt, "cmd.exe")]
    [InlineData(RuntimeKind.PowerShell, "powershell.exe")]
    public void BuildStartInfo_UsesRuntimeExecutable(RuntimeKind runtime, string expectedFileName)
    {
        var command = new PluginCommand { Executable = "opencode", Arguments = "--continue" };

        var (fileName, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, @"C:\proj");

        Assert.Equal(expectedFileName, fileName);
        Assert.Contains("opencode", arguments);
        Assert.Contains("--continue", arguments);
    }

    [Fact]
    public void BuildStartInfo_WindowsTerminal_IncludesWorkingDirectory()
    {
        var command = new PluginCommand { Executable = "opencode" };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(
            RuntimeKind.WindowsTerminal, command, @"C:\proj");

        Assert.Contains("-d \"C:\\proj\"", arguments);
        Assert.Contains("cmd /k opencode", arguments);
    }

    [Fact]
    public void SetRuntime_ThenGetRuntime_ReturnsConfiguredRuntime()
    {
        var launcher = new ProcessLauncher(NullLogger<ProcessLauncher>.Instance);

        launcher.SetRuntime(RuntimeKind.PowerShell);

        Assert.Equal(RuntimeKind.PowerShell, launcher.GetRuntime());
    }

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
