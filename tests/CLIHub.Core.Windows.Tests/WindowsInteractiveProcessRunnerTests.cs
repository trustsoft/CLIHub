namespace CLIHub.Core.Windows.Tests;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

public class WindowsInteractiveProcessRunnerTests
{
    [Fact]
    public void CreateStartInfo_SetsWorkingDirectory()
    {
        var runtime = new RuntimeInfo { ExecutablePath = "powershell.exe", Type = RuntimeType.PowerShell };

        var startInfo = WindowsInteractiveProcessRunner.CreateStartInfo(
            "-NoExit", runtime, @"C:\Projects\Demo Folder");

        Assert.Equal("powershell.exe", startInfo.FileName);
        Assert.Equal("-NoExit", startInfo.Arguments);
        Assert.Equal(@"C:\Projects\Demo Folder", startInfo.WorkingDirectory);
        Assert.False(startInfo.UseShellExecute);
    }

    [Fact]
    public async Task LaunchAsync_CommandPrompt_ChildStartsInWorkingDirectory()
    {
        await this.VerifyChildWorkingDirectoryAsync(
            RuntimeKind.CommandPrompt,
            arguments => arguments.Replace("/k", "/c", StringComparison.Ordinal),
            new RuntimeInfo { ExecutablePath = "cmd.exe", Type = RuntimeType.Cmd });
    }

    [Fact]
    public async Task LaunchAsync_PowerShell_ChildStartsInWorkingDirectory()
    {
        await this.VerifyChildWorkingDirectoryAsync(
            RuntimeKind.PowerShell,
            arguments => arguments.Replace("-NoExit ", string.Empty, StringComparison.Ordinal),
            new RuntimeInfo { ExecutablePath = "powershell.exe", Type = RuntimeType.PowerShell });
    }

    [Fact]
    public async Task LaunchAsync_PowerShell7_ChildStartsInWorkingDirectory()
    {
        if (!IsOnPath("pwsh.exe"))
        {
            return;
        }

        await this.VerifyChildWorkingDirectoryAsync(
            RuntimeKind.PowerShell7,
            arguments => arguments.Replace("-NoExit ", string.Empty, StringComparison.Ordinal),
            new RuntimeInfo { ExecutablePath = "pwsh.exe", Type = RuntimeType.PowerShell });
    }

    [Fact]
    public async Task LaunchAsync_GitBash_ChildStartsInWorkingDirectory()
    {
        if (!IsOnPath("bash.exe"))
        {
            return;
        }

        // Hand-built probe: the builder's command construction for Git Bash is covered by unit
        // tests, and its cmd-style escaping does not translate redirect characters, so a builder
        // driven probe cannot write the report file. What is verified here is the same OS-level
        // working-directory propagation for bash.exe as for the other runtimes.
        var workingDirectory = Path.Combine(
            Path.GetTempPath(), "clihub launch dir " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var reportPath = Path.Combine(workingDirectory, "cwd-report.txt");

        try
        {
            var arguments = $"-c \"pwd > '{ToPosixPath(reportPath)}'\"";
            var runtime = new RuntimeInfo { ExecutablePath = "bash.exe", Type = RuntimeType.Cmd };
            var runner = new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance);

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var exitCode = await runner.LaunchAsync(arguments, runtime, workingDirectory, cancellation.Token);

            Assert.Equal(0, exitCode);
            var reported = NormalizeReportedPath((await File.ReadAllTextAsync(reportPath)).Trim());
            Assert.Equal(
                Path.GetFullPath(workingDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(reported).TrimEnd(Path.DirectorySeparatorChar));
        }
        finally
        {
            try { Directory.Delete(workingDirectory, recursive: true); } catch { }
        }
    }

    /// <summary>
    ///   Runs the exact command line the runtime builder produces (with keep-open flags swapped
    ///   for headless ones) and proves the child process starts in the requested folder. The
    ///   probe command prints its working directory into a report file next to a script whose
    ///   path contains spaces.
    /// </summary>
    private async Task VerifyChildWorkingDirectoryAsync(
        RuntimeKind runtimeKind,
        Func<string, string> makeHeadless,
        RuntimeInfo runtime)
    {
        // The folder name contains spaces on purpose: that shape broke before the fix.
        var workingDirectory = Path.Combine(
            Path.GetTempPath(), "clihub launch dir " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var reportPath = Path.Combine(workingDirectory, "cwd-report.txt");
        var scriptPath = Path.Combine(workingDirectory, "print-cwd.cmd");

        try
        {
            await File.WriteAllTextAsync(scriptPath, "@cd>\"%~dp0cwd-report.txt\"");
            var command = new PluginCommand { Name = "launch", Executable = scriptPath };

            var (_, arguments) = ProcessLauncher.BuildStartInfo(runtimeKind, command, workingDirectory);
            arguments = makeHeadless(arguments);

            var runner = new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance);

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var exitCode = await runner.LaunchAsync(arguments, runtime, workingDirectory, cancellation.Token);

            Assert.Equal(0, exitCode);
            var reported = NormalizeReportedPath((await File.ReadAllTextAsync(reportPath)).Trim());
            Assert.Equal(
                Path.GetFullPath(workingDirectory).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(reported).TrimEnd(Path.DirectorySeparatorChar));
        }
        finally
        {
            try { Directory.Delete(workingDirectory, recursive: true); } catch { }
        }
    }

    private static string NormalizeReportedPath(string reported)
    {
        // Git Bash reports POSIX paths (/c/Users/...); convert them back to Windows form.
        if (reported.Length >= 3 && reported[0] == '/' && reported[2] == '/')
        {
            return char.ToUpperInvariant(reported[1]) + ":" + reported[2..].Replace('/', '\\');
        }

        return reported.Trim('"');
    }

    private static string ToPosixPath(string windowsPath)
    {
        var normalized = windowsPath.Replace('\\', '/');
        return normalized.Length >= 2 && normalized[1] == ':'
            ? "/" + char.ToLowerInvariant(normalized[0]) + normalized[2..]
            : normalized;
    }

    private static bool IsOnPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(directory => File.Exists(Path.Combine(directory.Trim(), executable)));
    }
}
