namespace CLIHub.Core.Windows;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Windows implementation of interactive process launching.
/// </summary>
public sealed class WindowsInteractiveProcessRunner : IInteractiveProcessRunner
{
    private readonly ILogger<WindowsInteractiveProcessRunner> logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsInteractiveProcessRunner" /> class.
    /// </summary>
    /// <param name="logger"> Logger for process operations. </param>
    public WindowsInteractiveProcessRunner(ILogger<WindowsInteractiveProcessRunner> logger)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> LaunchAsync(
        string commandLine,
        RuntimeInfo runtime,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);

        this.logger.LogDebug(
            "Launching interactive process in {WorkingDirectory}: {Runtime} {CommandLine}",
            workingDirectory, runtime.ExecutablePath, commandLine);

        using var process = new Process { StartInfo = CreateStartInfo(commandLine, runtime, workingDirectory) };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {runtime.ExecutablePath}");
            }

            this.logger.LogDebug("Process started with PID {ProcessId}", process.Id);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            int exitCode = process.ExitCode;
            this.logger.LogDebug("Process {ProcessId} exited with code {ExitCode}", process.Id, exitCode);

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            this.logger.LogWarning("Process launch was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to launch interactive process: {Runtime} {CommandLine}", runtime.ExecutablePath, commandLine);
            throw new InvalidOperationException($"Failed to launch process: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///   Creates the start info for an interactive launch, carrying the working directory
    ///   so the shell starts in the requested folder before running any command.
    /// </summary>
    /// <param name="commandLine"> The full command line to execute (already quoted/escaped). </param>
    /// <param name="runtime"> Runtime information specifying the executable and type. </param>
    /// <param name="workingDirectory"> The working directory the process starts in. </param>
    /// <returns> The configured start info. </returns>
    internal static ProcessStartInfo CreateStartInfo(string commandLine, RuntimeInfo runtime, string workingDirectory)
    {
        return new ProcessStartInfo
        {
            FileName = runtime.ExecutablePath,
            Arguments = commandLine,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };
    }
}
