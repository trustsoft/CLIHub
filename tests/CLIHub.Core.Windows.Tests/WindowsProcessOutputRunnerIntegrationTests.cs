namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Integration tests for <see cref="WindowsProcessOutputRunner" /> with real cmd.exe processes.
/// </summary>
public class WindowsProcessOutputRunnerIntegrationTests
{
    [Fact]
    public async Task RunAsync_CapturesStandardOutput()
    {
        var runner = new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance);
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("echo", "hello-capture");
        var runtime = new RuntimeInfo { ExecutablePath = fileName, Type = RuntimeType.Cmd };

        var result = await runner.RunAsync(arguments, runtime, TimeSpan.FromSeconds(10));

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello-capture", result.StandardOutput);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task RunAsync_MissingExecutable_ReportsFailure()
    {
        var runner = new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance);
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("this-command-does-not-exist-xyz", null);
        var runtime = new RuntimeInfo { ExecutablePath = fileName, Type = RuntimeType.Cmd };

        var result = await runner.RunAsync(arguments, runtime, TimeSpan.FromSeconds(10));

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task RunAsync_CallerCancellationTerminatesProcess()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var runner = new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance);
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("ping", "-n 10 127.0.0.1");
        var runtime = new RuntimeInfo { ExecutablePath = fileName, Type = RuntimeType.Cmd };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await runner.RunAsync(arguments, runtime, TimeSpan.FromSeconds(30), cancellation.Token));
    }

    [Fact]
    public async Task RunAsync_TimeoutTerminatesProcess()
    {
        var runner = new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance);
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("ping", "-n 10 127.0.0.1");
        var runtime = new RuntimeInfo { ExecutablePath = fileName, Type = RuntimeType.Cmd };

        var result = await runner.RunAsync(arguments, runtime, TimeSpan.FromMilliseconds(100));

        Assert.True(result.TimedOut);
        Assert.Equal(-1, result.ExitCode);
    }

    [Theory]
    [InlineData(".cmd")]
    [InlineData(".bat")]
    public async Task RunAsync_ResolvesShellShim(string extension)
    {
        var directory = Path.Combine(Path.GetTempPath(), "clihub-shim-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var executable = Path.Combine(directory, "shim" + extension);

        try
        {
            await File.WriteAllTextAsync(executable, "@echo shim-output");
            var runner = new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance);
            var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand(executable, null);
            var runtime = new RuntimeInfo { ExecutablePath = fileName, Type = RuntimeType.Cmd };

            var result = await runner.RunAsync(arguments, runtime, TimeSpan.FromSeconds(10));

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("shim-output", result.StandardOutput);
            Assert.False(result.TimedOut);
        }
        finally
        {
            try { Directory.Delete(directory, recursive: true); } catch { }
        }
    }
}
