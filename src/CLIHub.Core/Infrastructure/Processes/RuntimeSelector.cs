namespace CLIHub.Core.Infrastructure.Processes;

using System;
using System.IO;

/// <summary>
///   Detects and selects the appropriate command-line runtime on Windows.
/// </summary>
public sealed class RuntimeSelector
{
    private RuntimeInfo? _cachedRuntime;
    private readonly object _lock = new();

    /// <summary>
    ///   Selects the best available runtime for executing commands.
    /// </summary>
    /// <returns> Information about the selected runtime. </returns>
    public RuntimeInfo SelectRuntime()
    {
        if (_cachedRuntime is not null)
        {
            return _cachedRuntime;
        }

        lock (_lock)
        {
            if (_cachedRuntime is not null)
            {
                return _cachedRuntime;
            }

            _cachedRuntime = DetectRuntime();
            return _cachedRuntime;
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
