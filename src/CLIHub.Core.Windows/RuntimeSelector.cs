namespace CLIHub.Core.Windows;

using System;
using System.IO;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Detects and selects the appropriate command-line runtime on Windows.
/// </summary>
public sealed class RuntimeSelector
{
    private RuntimeInfo? cachedRuntime;
    private readonly object gate = new();

    /// <summary>
    ///   Selects the best available runtime for executing commands.
    /// </summary>
    /// <returns> Information about the selected runtime. </returns>
    public RuntimeInfo SelectRuntime()
    {
        if (this.cachedRuntime is not null)
        {
            return this.cachedRuntime;
        }

        lock (this.gate)
        {
            if (this.cachedRuntime is not null)
            {
                return this.cachedRuntime;
            }

            this.cachedRuntime = DetectRuntime();
            return this.cachedRuntime;
        }
    }

    private static RuntimeInfo DetectRuntime()
    {
        // Check if Windows Terminal is available in PATH
        if (IsExecutableInPath("wt.exe"))
        {
            return new RuntimeInfo
            {
                ExecutablePath = "wt.exe",
                Type = RuntimeType.WindowsTerminal
            };
        }

        // Fall back to CMD
        return new RuntimeInfo
        {
            ExecutablePath = "cmd.exe",
            Type = RuntimeType.Cmd
        };
    }

    private static bool IsExecutableInPath(string executable)
    {
        try
        {
            string? pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(pathEnv))
            {
                return false;
            }

            string[] paths = pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string path in paths)
            {
                string fullPath = Path.Combine(path, executable);
                if (File.Exists(fullPath))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
