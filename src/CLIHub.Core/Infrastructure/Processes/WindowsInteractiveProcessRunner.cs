namespace CLIHub.Core.Infrastructure.Processes;

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

/// <summary>
///   Windows implementation of interactive process launching.
/// </summary>
public sealed class WindowsInteractiveProcessRunner : IInteractiveProcessRunner
{
    private readonly ILogger<WindowsInteractiveProcessRunner> _logger;

    /// <summary>
    ///   Initializes a new instance of the <see cref="WindowsInteractiveProcessRunner"/> class.
    /// </summary>
    /// <param name="logger"> Logger for process operations. </param>
    public WindowsInteractiveProcessRunner(ILogger<WindowsInteractiveProcessRunner> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<int> LaunchAsync(
        string commandLine,
        RuntimeInfo runtime,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(commandLine);
        ArgumentNullException.ThrowIfNull(runtime);

        _logger.LogDebug("Launching interactive process: {Runtime} {CommandLine}", runtime.ExecutablePath, commandLine);

        var startInfo = new ProcessStartInfo
        {
            FileName = runtime.ExecutablePath,
            Arguments = commandLine,
            UseShellExecute = false,
            RedirectStandardInput = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {runtime.ExecutablePath}");
            }

            _logger.LogDebug("Process started with PID {ProcessId}", process.Id);

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

            int exitCode = process.ExitCode;
            _logger.LogDebug("Process {ProcessId} exited with code {ExitCode}", process.Id, exitCode);

            return exitCode;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Process launch was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch interactive process: {Runtime} {CommandLine}", runtime.ExecutablePath, commandLine);
            throw new InvalidOperationException($"Failed to launch process: {ex.Message}", ex);
        }
    }
}
