using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;
using System.Diagnostics;

namespace CLIHub.Core.Services;

/// <summary>
/// Service for launching processes for AI agent CLI tools
/// </summary>
public class ProcessLauncher : IProcessLauncher
{
    private string _terminalExecutable;

    public ProcessLauncher()
    {
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
            Console.WriteLine("Error: Plugin command cannot be null");
            return false;
        }

        if (string.IsNullOrWhiteSpace(command.Executable))
        {
            Console.WriteLine("Error: Executable path is required in plugin command");
            return false;
        }

        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            Console.WriteLine("Error: Working directory cannot be null or empty");
            return false;
        }

        if (!Directory.Exists(workingDirectory))
        {
            Console.WriteLine($"Error: Working directory does not exist: {workingDirectory}");
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _terminalExecutable,
                Arguments = BuildTerminalArguments(command, workingDirectory),
                WorkingDirectory = workingDirectory,
                UseShellExecute = true // Required for wt.exe to work properly
            };

            Process.Start(startInfo);
            Console.WriteLine($"Successfully launched: {command.Name} in {workingDirectory}");
            return true;
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            Console.WriteLine($"Error: Failed to start {command.Executable} - {ex.Message}");
            if (ex.Message.Contains("not found"))
            {
                Console.WriteLine("Hint: Make sure the executable is in your PATH or provide a full path");
            }
            return false;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Error: Access denied when starting {command.Executable} - {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error launching process: {ex.GetType().Name} - {ex.Message}");
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