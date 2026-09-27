using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CLIHub.Core.Services;

/// <summary>
/// Service for launching processes for AI agent CLI tools
/// </summary>
public class ProcessLauncher : IProcessLauncher
{
    private readonly ILogger<ProcessLauncher> _logger;
    private string _terminalExecutable;

    public ProcessLauncher(ILogger<ProcessLauncher> logger)
    {
        _logger = logger;
        // Default to Windows Terminal
        _terminalExecutable = "wt.exe";
    }

    /// <summary>
    /// Sets the terminal executable to use for launching processes
    /// </summary>
    /// <param name="terminalExecutable">Path to the terminal executable (e.g., "wt.exe", "cmd.exe")</param>
    public void SetTerminalExecutable(string terminalExecutable)
    {
        if (!string.IsNullOrWhiteSpace(terminalExecutable))
        {
            _terminalExecutable = terminalExecutable;
        }
    }

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

        var arguments = BuildTerminalArguments(command, workingDirectory);
        _logger.LogInformation(
            "Launching {CommandName} ({Executable}) in {WorkingDirectory}",
            command.Name, command.Executable, workingDirectory);

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _terminalExecutable,
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

    private string BuildTerminalArguments(PluginCommand command, string workingDirectory)
    {
        // Windows Terminal syntax: wt.exe -d "path" cmd /k "command arguments"
        var executable = string.IsNullOrEmpty(command.Executable) ? "cmd" : command.Executable;
        
        // Handle arguments: if they exist, escape quotes and add them
        var arguments = string.Empty;
        if (!string.IsNullOrEmpty(command.Arguments))
        {
            // Escape quotes in arguments for command line
            var escapedArgs = command.Arguments.Replace("\"", "\\\"");
            arguments = $" {escapedArgs}";
        }
        
        return $"-d \"{workingDirectory}\" cmd /k {executable}{arguments}";
    }

    public string GetTerminalExecutable()
    {
        return _terminalExecutable;
    }
}
