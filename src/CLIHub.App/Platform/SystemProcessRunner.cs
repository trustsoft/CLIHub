using System.Diagnostics;
using CLIHub.Core.Abstractions;

namespace CLIHub.App.Platform;

public sealed class SystemProcessRunner : IProcessRunner
{
    public bool CommandExists(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.CMD;.BAT")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var bareName = Path.GetFileNameWithoutExtension(command);

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (File.Exists(Path.Combine(directory, command)))
            {
                return true;
            }

            foreach (var extension in extensions)
            {
                if (File.Exists(Path.Combine(directory, bareName + extension)))
                {
                    return true;
                }
            }
        }

        return false;
    }

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
}
