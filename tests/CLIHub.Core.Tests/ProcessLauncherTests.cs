namespace CLIHub.Tests.Services;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Infrastructure.Processes;

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
        if (runtime == RuntimeKind.PowerShell)
        {
            var encoded = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
            var script = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded));
            Assert.Contains("opencode", script);
            Assert.Contains("--continue", script);
        }
        else
        {
            Assert.Contains("opencode", arguments);
            Assert.Contains("--continue", arguments);
        }
    }

    [Fact]
    public void BuildStartInfo_WindowsTerminal_IncludesWorkingDirectory()
    {
        var command = new PluginCommand { Executable = "opencode" };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(
            RuntimeKind.WindowsTerminal, command, @"C:\proj");

        Assert.Contains("-d \"C:\\proj\"", arguments);
        Assert.Contains("cmd /d /k \"opencode\"", arguments);
    }

    [Fact]
    public void BuildStartInfo_QuotesExecutablePathAndShellCharacters()
    {
        var command = new PluginCommand
        {
            Executable = @"C:\Program Files\Agent\agent.exe",
            Arguments = "--name \"A&B\""
        };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(
            RuntimeKind.CommandPrompt, command, @"C:\Projects\Demo Folder");

        Assert.Contains("\"C:\\Program Files\\Agent\\agent.exe\"", arguments);
        Assert.Contains("^&", arguments);
    }

    [Fact]
    public void CommandLineBuilder_QuotesExecutableAndEscapesArgumentTail()
    {
        var command = new PluginCommand
        {
            Executable = @"C:\Program Files\Agent\agent.exe",
            Arguments = "--name \"A&B\""
        };

        var commandLine = WindowsCommandLineBuilder.BuildCommandLine(command);

        Assert.Equal("\"C:\\Program Files\\Agent\\agent.exe\" --name \"A^&B\"", commandLine);
    }

    [Fact]
    public void CommandLineBuilder_BuildsCapturedCommandThroughComSpec()
    {
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("echo", "hello & world");

        Assert.Equal(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", fileName);
        Assert.Contains("/d /s /c", arguments);
        Assert.Contains("hello ^^^& world", arguments);
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

    [Fact]
    public async Task CaptureOutputAsync_CallerCancellationTerminatesProcessAndReportsCancellation()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var result = await new ProcessLauncher(NullLogger<ProcessLauncher>.Instance)
            .CaptureOutputAsync("ping", "-n 10 127.0.0.1", Path.GetTempPath(), cancellation.Token, TimeSpan.FromSeconds(30));

        Assert.False(result.Started);
        Assert.Equal("Process cancelled", result.StdErr);
    }

    [Fact]
    public async Task CaptureOutputAsync_TimeoutTerminatesProcessAndReportsTimeout()
    {
        var result = await new ProcessLauncher(NullLogger<ProcessLauncher>.Instance)
            .CaptureOutputAsync("ping", "-n 10 127.0.0.1", Path.GetTempPath(), timeout: TimeSpan.FromMilliseconds(100));

        Assert.False(result.Started);
        Assert.Equal("Timed out waiting for the command", result.StdErr);
    }

    [Theory]
    [InlineData(".cmd")]
    [InlineData(".bat")]
    public async Task CaptureOutputAsync_ResolvesShellShim(string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), "clihub-shim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var executable = Path.Combine(directory, "shim" + extension);

        try
        {
            await File.WriteAllTextAsync(executable, "@echo shim-output");
            var result = await new ProcessLauncher(NullLogger<ProcessLauncher>.Instance)
                .CaptureOutputAsync(executable, null, directory);

            Assert.True(result.Started);
            Assert.Equal(0, result.ExitCode);
            Assert.Contains("shim-output", result.StdOut);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }
}
