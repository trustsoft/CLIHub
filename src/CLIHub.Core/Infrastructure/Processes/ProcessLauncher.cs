namespace CLIHub.Core.Infrastructure.Processes;

using System.Diagnostics;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

/// <summary>
///   Launches processes for AI agent CLI tools.
/// </summary>
public class ProcessLauncher : IInteractiveProcessRunner, IProcessOutputRunner
{
    private static readonly TimeSpan DefaultCaptureTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<ProcessLauncher> _logger;
    private RuntimeKind _runtime = RuntimeKind.WindowsTerminal;

    /// <summary>
    ///   Creates the launcher.
    /// </summary>
    public ProcessLauncher(ILogger<ProcessLauncher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///   Sets the runtime used to launch interactive agent commands.
    /// </summary>
    public void SetRuntime(RuntimeKind runtime) => _runtime = runtime;

    /// <summary>
    ///   Gets the configured runtime.
    /// </summary>
    public RuntimeKind GetRuntime() => _runtime;

    /// <inheritdoc />
    public bool LaunchProcess(PluginCommand command, string workingDirectory)
    {
        if (command == null)
        {
            _logger.LogError("Plugin command cannot be null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.Executable))
        {
            _logger.LogError("Executable path is required in plugin command {CommandName}", command.Name);
            return false;
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            _logger.LogError("Working directory cannot be null or empty");
            return false;
        }

        if (!Directory.Exists(workingDirectory))
        {
            _logger.LogError("Working directory does not exist: {WorkingDirectory}", workingDirectory);
            return false;
        }

        var (fileName, arguments) = BuildStartInfo(_runtime, command, workingDirectory);
        _logger.LogInformation(
            "Launching {CommandName} ({Executable}) in {WorkingDirectory} via {Runtime}",
            command.Name, command.Executable, workingDirectory, _runtime);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = true // Required for wt.exe to work properly
            };

            Process.Start(startInfo);
            _logger.LogInformation("Launched {CommandName} in {WorkingDirectory}", command.Name, workingDirectory);
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start {Executable}", command.Executable);
            if (ex.Message.Contains("not found"))
            {
                _logger.LogWarning("Make sure {Executable} is on PATH or provide a full path", command.Executable);
            }
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Access denied when starting {Executable}", command.Executable);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error launching process");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ProcessCaptureResult> CaptureOutputAsync(
        string executable,
        string? arguments,
        string workingDirectory,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return new ProcessCaptureResult(false, -1, string.Empty, "Executable is required");
        }

        try
        {
            // Run through the command interpreter so PATHEXT resolution works for
            // npm/shim executables (.cmd/.bat/.ps1), which CreateProcess cannot resolve.
            var (fileName, commandLineArguments) = WindowsCommandLineBuilder.BuildCapturedCommand(executable, arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = commandLineArguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return new ProcessCaptureResult(false, -1, string.Empty, "Process did not start");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout ?? DefaultCaptureTimeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                await TerminateProcessAsync(process);
                var reason = cancellationToken.IsCancellationRequested
                    ? "Process cancelled"
                    : "Timed out waiting for the command";
                return new ProcessCaptureResult(false, -1, string.Empty, reason);
            }

            var stdout = (await stdoutTask).Trim();
            var stderr = (await stderrTask).Trim();
            return new ProcessCaptureResult(true, process.ExitCode, stdout, stderr);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to capture output from {Executable}", executable);
            return new ProcessCaptureResult(false, -1, string.Empty, ex.Message);
        }
    }

    /// <summary>
    ///   Builds the process file name and arguments for the given runtime.
    /// </summary>
    internal static (string FileName, string Arguments) BuildStartInfo(
        RuntimeKind runtime,
        PluginCommand command,
        string workingDirectory)
    {
        return WindowsCommandLineBuilder.BuildInteractive(runtime, command, workingDirectory);
    }

    /// <summary>
    ///   Builds the shell command line for a plugin command (executable plus escaped arguments).
    /// </summary>
    internal static string BuildCommandLine(PluginCommand command)
    {
        return WindowsCommandLineBuilder.BuildCommandLine(command);
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
            // Cleanup is best effort; the capture still reports the original cancellation reason.
        }
    }
}
