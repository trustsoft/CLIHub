namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Processes;

public class ProcessLauncherCaptureOutputTests
{
    [Fact]
    public async Task CaptureOutputAsync_CapturesStandardOutput()
    {
        var launcher = CreateRealProcessLauncher();

        var result = await launcher.CaptureOutputAsync("echo", "hello-capture", Path.GetTempPath());

        Assert.True(result.Started);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("hello-capture", result.StdOut);
    }

    [Fact]
    public async Task CaptureOutputAsync_MissingExecutable_ReportsFailure()
    {
        var launcher = CreateRealProcessLauncher();

        var result = await launcher.CaptureOutputAsync(
            "this-command-does-not-exist-xyz", null, Path.GetTempPath());

        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task CaptureOutputAsync_CallerCancellationTerminatesProcessAndReportsCancellation()
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var result = await CreateRealProcessLauncher()
            .CaptureOutputAsync("ping", "-n 10 127.0.0.1", Path.GetTempPath(), cancellation.Token, TimeSpan.FromSeconds(30));

        Assert.False(result.Started);
        Assert.Equal("Process cancelled", result.StdErr);
    }

    [Fact]
    public async Task CaptureOutputAsync_TimeoutTerminatesProcessAndReportsTimeout()
    {
        var result = await CreateRealProcessLauncher()
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
            var result = await CreateRealProcessLauncher()
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

    private static ProcessLauncher CreateRealProcessLauncher()
    {
        return new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance),
            new WindowsProcessOutputRunner(NullLogger<WindowsProcessOutputRunner>.Instance));
    }
}
