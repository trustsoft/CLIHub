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
        var locator = new WindowsRuntimeLocator(
            new WindowsRuntimeEnvironment(),
            NullLogger<WindowsRuntimeLocator>.Instance);
        var resolvedPwsh = locator.Locate(RuntimeKind.PowerShell7);
        if (resolvedPwsh is null)
        {
            // PowerShell 7 is not installed on this machine; there is nothing to launch.
            return;
        }

        await this.VerifyChildWorkingDirectoryAsync(
            RuntimeKind.PowerShell7,
            arguments => arguments.Replace("-NoExit ", string.Empty, StringComparison.Ordinal),
            new RuntimeInfo { ExecutablePath = resolvedPwsh, Type = RuntimeType.PowerShell },
            resolvedPwsh);
    }

    [Fact]
    public async Task LaunchAsync_GitBash_ChildStartsInWorkingDirectory()
    {
        var locator = new WindowsRuntimeLocator(
            new WindowsRuntimeEnvironment(),
            NullLogger<WindowsRuntimeLocator>.Instance);
        var resolvedGitBash = locator.Locate(RuntimeKind.GitBash);
        if (resolvedGitBash is null)
        {
            // Git for Windows is not installed on this machine; there is nothing to launch.
            return;
        }

        var workingDirectory = Path.Combine(
            Path.GetTempPath(), "clihub launch dir " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);
        var reportPath = Path.Combine(workingDirectory, "cwd-report.txt");

        try
        {
            // The resolver contract is exercised end to end here: the resolved Git for Windows
            // bash.exe is launched and the child starts in the requested folder. Because the
            // executable comes from the resolver, the WSL launcher can no longer be selected.
            var runtime = new RuntimeInfo { ExecutablePath = resolvedGitBash, Type = RuntimeType.Cmd };
            var runner = new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance);

            // The probe writes to a relative path, so the report lands in the working directory
            // the operating system gave the process no matter which bash flavor resolves: Git
            // Bash reports POSIX paths under /c/.... Asserting that the report exists inside the
            // requested folder and names it proves the child started there.
            var arguments = "-c \"pwd > cwd-report.txt\"";

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var exitCode = await runner.LaunchAsync(arguments, runtime, workingDirectory, cancellation.Token);

            Assert.Equal(0, exitCode);
            var reported = (await File.ReadAllTextAsync(reportPath)).Trim().Trim('"');
            Assert.Contains("clihub launch dir", reported);

            var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            Assert.False(
                resolvedGitBash.StartsWith(windowsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                $"The Git Bash runtime must not resolve inside the Windows directory: {resolvedGitBash}");
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
    [Fact]
    public async Task LaunchAsync_PowerShell_PathResolvedShim_ReceivesArgumentsWithoutStopParsingToken()
    {
        // Simulates an npm-style agent: a bare executable name on PATH that resolves to a
        // .cmd shim. The builder must resolve the name to a native application so the
        // stop-parsing token is consumed by PowerShell and never reaches the agent.
        var shimDir = Path.Combine(Path.GetTempPath(), "clihub-shim-path-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(shimDir);
        var reportPath = Path.Combine(shimDir, "args-report.txt");
        var shimPath = Path.Combine(shimDir, "probe-agent.cmd");
        await File.WriteAllTextAsync(shimPath, "@echo ARGS=%*>\"%~dp0args-report.txt\"");

        var originalPath = Environment.GetEnvironmentVariable("PATH");
        Environment.SetEnvironmentVariable("PATH", shimDir + Path.PathSeparator + originalPath);

        try
        {
            var command = new PluginCommand { Name = "resume", Executable = "probe-agent", Arguments = "--continue" };
            var (_, arguments) = WindowsCommandLineBuilder.BuildInteractive(
                RuntimeKind.PowerShell, command, shimDir, gitBashExecutable: null, powerShell7Executable: null);
            arguments = arguments.Replace("-NoExit ", string.Empty, StringComparison.Ordinal);

            var runtime = new RuntimeInfo { ExecutablePath = "powershell.exe", Type = RuntimeType.PowerShell };
            var runner = new WindowsInteractiveProcessRunner(NullLogger<WindowsInteractiveProcessRunner>.Instance);

            using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var exitCode = await runner.LaunchAsync(arguments, runtime, shimDir, cancellation.Token);

            Assert.Equal(0, exitCode);
            var reported = (await File.ReadAllTextAsync(reportPath)).Trim();
            Assert.Contains("ARGS=--continue", reported);
            Assert.DoesNotContain("--%", reported);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PATH", originalPath);
            try { Directory.Delete(shimDir, recursive: true); } catch { }
        }
    }

    private async Task VerifyChildWorkingDirectoryAsync(
        RuntimeKind runtimeKind,
        Func<string, string> makeHeadless,
        RuntimeInfo runtime,
        string? powerShell7Executable = null)
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

            var (_, arguments) = WindowsCommandLineBuilder.BuildInteractive(
                runtimeKind, command, workingDirectory, gitBashExecutable: null, powerShell7Executable);
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
}
