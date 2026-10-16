namespace CLIHub.Core.Infrastructure.Processes;

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using CLIHub.Core.Models;

/// <summary>
///   Launches processes for AI agent CLI tools.
/// </summary>
public sealed class ProcessLauncher
{
    private static readonly TimeSpan DefaultCaptureTimeout = TimeSpan.FromSeconds(10);

    private readonly ILogger<ProcessLauncher> logger;
    private readonly IInteractiveProcessRunner interactiveRunner;
    private readonly IProcessOutputRunner outputRunner;
    
    private RuntimeKind runtime = RuntimeKind.PowerShell;

    /// <summary>
    ///   Creates the launcher.
    /// </summary>
    public ProcessLauncher(
        ILogger<ProcessLauncher> logger,
        IInteractiveProcessRunner interactiveRunner,
        IProcessOutputRunner outputRunner)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.interactiveRunner = interactiveRunner ?? throw new ArgumentNullException(nameof(interactiveRunner));
        this.outputRunner = outputRunner ?? throw new ArgumentNullException(nameof(outputRunner));
    }

    /// <summary>
    ///   Sets the runtime used to launch interactive agent commands.
    /// </summary>
    public void SetRuntime(RuntimeKind runtimeKind) => this.runtime = runtimeKind;

    /// <summary>
    ///   Gets the configured runtime.
    /// </summary>
    public RuntimeKind GetRuntime() => this.runtime;

    /// <summary>
    ///   Launches a process for the specified plugin command in the given project directory.
    /// </summary>
    /// <param name="command"> The plugin command to execute. </param>
    /// <param name="workingDirectory"> The working directory for the process. </param>
    /// <returns> True when the process was launched successfully; otherwise false. </returns>
    public bool LaunchProcess(PluginCommand command, string workingDirectory)
    {
        if (command == null)
        {
            this.logger.LogError("Plugin command cannot be null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.Executable))
        {
            this.logger.LogError("Executable path is required in plugin command {CommandName}", command.Name);
            return false;
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            this.logger.LogError("Working directory cannot be null or empty");
            return false;
        }

        if (!Directory.Exists(workingDirectory))
        {
            this.logger.LogError("Working directory does not exist: {WorkingDirectory}", workingDirectory);
            return false;
        }

        this.logger.LogInformation(
            "Launching {CommandName} ({Executable}) in {WorkingDirectory} via {Runtime}",
            command.Name, command.Executable, workingDirectory, this.runtime);

        try
        {
            // Build command line for the runtime
            var (fileName, arguments) = BuildStartInfo(this.runtime, command, workingDirectory);
            
            // Create runtime info
            var runtimeType = this.runtime switch
            {
                RuntimeKind.CommandPrompt => RuntimeType.Cmd,
                RuntimeKind.PowerShell or RuntimeKind.PowerShell7 => RuntimeType.PowerShell,
                _ => RuntimeType.Cmd
            };
            
            var runtime = new RuntimeInfo
            {
                ExecutablePath = fileName,
                Type = runtimeType
            };

            // Delegate to the interactive runner
            _ = this.interactiveRunner.LaunchAsync(arguments, runtime, CancellationToken.None);
            this.logger.LogInformation("Launched {CommandName} in {WorkingDirectory}", command.Name, workingDirectory);
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            this.logger.LogError(ex, "Failed to start {Executable}", command.Executable);
            if (ex.Message.Contains("not found"))
            {
                this.logger.LogWarning("Make sure {Executable} is on PATH or provide a full path", command.Executable);
            }
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            this.logger.LogError(ex, "Access denied when starting {Executable}", command.Executable);
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Unexpected error launching process");
            return false;
        }
    }

    /// <summary>
    ///   Runs an executable and captures its output.
    /// </summary>
    /// <param name="executable"> Executable to run. </param>
    /// <param name="arguments"> Optional command-line arguments. </param>
    /// <param name="workingDirectory"> Working directory for the process. </param>
    /// <param name="cancellationToken"> Cancellation token. </param>
    /// <param name="timeout"> Optional maximum run time. </param>
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
            // Get runtime (cmd.exe for output capture)
            var runtime = new RuntimeInfo 
            { 
                ExecutablePath = "cmd.exe", 
                Type = RuntimeType.Cmd 
            };

            // Build command line through cmd.exe for PATHEXT resolution
            var (_, commandLine) = WindowsCommandLineBuilder.BuildCapturedCommand(executable, arguments);
            
            // Set working directory via /D flag in command line
            var fullCommandLine = $"/D \"{workingDirectory}\" {commandLine}";

            // Run with output capture
            var result = await this.outputRunner.RunAsync(
                fullCommandLine,
                runtime,
                timeout ?? DefaultCaptureTimeout,
                cancellationToken);

            // Adapt ProcessResult to ProcessCaptureResult
            if (result.TimedOut)
            {
                var reason = cancellationToken.IsCancellationRequested
                    ? "Process cancelled"
                    : "Timed out waiting for the command";
                return new ProcessCaptureResult(false, result.ExitCode, result.StandardOutput, reason);
            }

            return new ProcessCaptureResult(
                true,
                result.ExitCode,
                result.StandardOutput,
                result.StandardError);
        }
        catch (OperationCanceledException)
        {
            return new ProcessCaptureResult(false, -1, string.Empty, "Process cancelled");
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to capture output from {Executable}", executable);
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
}
