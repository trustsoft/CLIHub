using System.Diagnostics;
using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.App.Platform;

public sealed class SystemProcessRunner : IProcessRunner
{
    public bool CommandExists(string command) => ResolveCommandPath(command) is not null;

    public void StartDetached(string fileName, string arguments, string workingDirectory)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public ProbeResult RunProbe(string fileName, string arguments, int timeoutSeconds)
    {
        var executable = ResolveCommandPath(fileName) ?? fileName;

        var actualFileName = executable;
        var actualArguments = arguments;

        var extension = Path.GetExtension(executable);
        if (string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase))
        {
            actualFileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
            actualArguments = arguments.Length == 0
                ? $"/c \"{executable}\""
                : $"/c \"{executable}\" {arguments}";
        }

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = actualFileName,
                Arguments = actualArguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return ProbeResult.Failed;
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();

        if (!process.WaitForExit(timeoutSeconds * 1000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                // best-effort kill
            }

            return new ProbeResult(-1, null, TimedOut: true);
        }

        return new ProbeResult(process.ExitCode, outputTask.Result, TimedOut: false);
    }

    private static string? ResolveCommandPath(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var bareName = Path.GetFileNameWithoutExtension(command);

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Path.GetExtension(command).Length > 0)
            {
                var direct = Path.Combine(directory, command);
                if (File.Exists(direct))
                {
                    return direct;
                }
            }

            foreach (var extension in extensions)
            {
                var withExtension = Path.Combine(directory, bareName + extension);
                if (File.Exists(withExtension))
                {
                    return withExtension;
                }
            }
        }

        return null;
    }
}
