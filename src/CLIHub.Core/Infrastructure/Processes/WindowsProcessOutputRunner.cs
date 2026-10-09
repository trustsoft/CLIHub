namespace CLIHub.Core.Infrastructure.Processes;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

/// <summary>
///   Windows implementation of process output runner with timeout support.
/// </summary>
public sealed class WindowsProcessOutputRunner : IProcessOutputRunner
{
    private readonly ILogger<WindowsProcessOutputRunner> _logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsProcessOutputRunner"/> class.
    /// </summary>
    /// <param name="logger"> Logger for process operations. </param>
    public WindowsProcessOutputRunner(ILogger<WindowsProcessOutputRunner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ProcessResult> RunAsync(
        string commandLine,
        RuntimeInfo runtime,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(runtime);

        _logger.LogDebug("Running process with output capture: {Runtime} {CommandLine}", runtime.ExecutablePath, commandLine);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = runtime.ExecutablePath,
                Arguments = commandLine,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Process did not start");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            bool timedOut = false;

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                timedOut = !cancellationToken.IsCancellationRequested;
                await TerminateProcessAsync(process);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }

            var stdout = (await stdoutTask).Trim();
            var stderr = (await stderrTask).Trim();
            int exitCode = timedOut ? -1 : process.ExitCode;

            _logger.LogDebug(
                "Process completed: ExitCode={ExitCode}, TimedOut={TimedOut}, StdOut={StdOutLength} chars, StdErr={StdErrLength} chars",
                exitCode, timedOut, stdout.Length, stderr.Length);

            return new ProcessResult
            {
                StandardOutput = stdout,
                StandardError = stderr,
                ExitCode = exitCode,
                TimedOut = timedOut
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to run process with command line: {CommandLine}", commandLine);
            throw new InvalidOperationException($"Failed to run process: {ex.Message}", ex);
        }
    }

    private static async Task TerminateProcessAsync(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch
        {
            // The process may have exited between cancellation and termination.
        }

        try
        {
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Cleanup is best effort.
        }
    }
}
