namespace CLIHub.Core.Tests.Infrastructure.Processes;

using Microsoft.Extensions.Logging.Abstractions;

using CLIHub.Core.Models;
using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Tests.Fakes;

public class ProcessLauncherTests
{
    [Theory]
    [InlineData(RuntimeKind.PowerShell, "powershell.exe")]
    [InlineData(RuntimeKind.PowerShell7, "pwsh.exe")]
    [InlineData(RuntimeKind.CommandPrompt, "cmd.exe")]
    [InlineData(RuntimeKind.GitBash, "bash.exe")]
    public void BuildStartInfo_UsesRuntimeExecutable(RuntimeKind runtime, string expectedFileName)
    {
        var command = new PluginCommand { Executable = "opencode", Arguments = "--continue" };

        var (fileName, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, @"C:\proj");

        Assert.Equal(expectedFileName, fileName);
        if (runtime is RuntimeKind.PowerShell or RuntimeKind.PowerShell7)
        {
            var encoded = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
            var script = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded));
            Assert.StartsWith("$exe = (Get-Command 'opencode' -CommandType Application", script);
            Assert.Contains("& $exe --% --continue", script);
            Assert.DoesNotContain("& 'opencode' --%", script);
            Assert.DoesNotContain("& &", script);
        }
        else
        {
            Assert.Contains("opencode", arguments);
            Assert.Contains("--continue", arguments);
        }
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.PowerShell7)]
    public void BuildStartInfo_PowerShell_EncodesSingleCallScript(RuntimeKind runtime)
    {
        var command = new PluginCommand { Executable = "opencode" };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, @"C:\proj");

        var encoded = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        var script = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded));

        Assert.Equal("& 'opencode'", script);
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.PowerShell7)]
    public void BuildStartInfo_PowerShell_BareNameWithArguments_ResolvesNativeApplication(RuntimeKind runtime)
    {
        var command = new PluginCommand { Executable = "opencode", Arguments = "--continue" };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, @"C:\proj");

        var encoded = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        var script = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded));

        Assert.Equal(
            "$exe = (Get-Command 'opencode' -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source\n" +
            "& $exe --% --continue",
            script);
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.PowerShell7)]
    public void BuildStartInfo_PowerShell_QuotedExecutableBuildsSingleCall(RuntimeKind runtime)
    {
        var command = new PluginCommand
        {
            Executable = @"C:\Program Files\Agent\agent.exe",
            Arguments = "--run"
        };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(runtime, command, @"C:\proj");

        var encoded = arguments.Split(' ', StringSplitOptions.RemoveEmptyEntries).Last();
        var script = System.Text.Encoding.Unicode.GetString(Convert.FromBase64String(encoded));

        Assert.Equal(@"& 'C:\Program Files\Agent\agent.exe' --% --run", script);
    }

    [Fact]
    public void BuildStartInfo_GitBash_UsesLoginInteractiveShell()
    {
        var command = new PluginCommand { Executable = "opencode", Arguments = "--continue" };

        var (fileName, arguments) = ProcessLauncher.BuildStartInfo(
            RuntimeKind.GitBash, command, @"C:\proj");

        Assert.Equal("bash.exe", fileName);
        Assert.Contains("-lic", arguments);
        Assert.Contains("opencode", arguments);
        Assert.Contains("--continue", arguments);
    }

    [Fact]
    public void BuildStartInfo_QuotesExecutablePathAndShellCharacters()
    {
        var command = new PluginCommand
        {
            Executable = @"C:\Program Files\Agent\agent.exe",
            Arguments = "--name \"A&B\""
        };

        var (_, arguments) = ProcessLauncher.BuildStartInfo(
            RuntimeKind.CommandPrompt, command, @"C:\Projects\Demo Folder");

        Assert.Contains("\"C:\\Program Files\\Agent\\agent.exe\"", arguments);
        Assert.Contains("^&", arguments);
    }

    [Fact]
    public void CommandLineBuilder_QuotesExecutableAndEscapesArgumentTail()
    {
        var command = new PluginCommand
        {
            Executable = @"C:\Program Files\Agent\agent.exe",
            Arguments = "--name \"A&B\""
        };

        var commandLine = WindowsCommandLineBuilder.BuildCommandLine(command);

        Assert.Equal("\"C:\\Program Files\\Agent\\agent.exe\" --name \"A^&B\"", commandLine);
    }

    [Fact]
    public void CommandLineBuilder_BuildsCapturedCommandThroughComSpec()
    {
        var (fileName, arguments) = WindowsCommandLineBuilder.BuildCapturedCommand("echo", "hello & world");

        Assert.Equal(Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe", fileName);
        Assert.Contains("/d /s /c", arguments);
        Assert.Contains("hello ^^^& world", arguments);
    }

    [Fact]
    public void LaunchProcess_PassesWorkingDirectoryToInteractiveRunner()
    {
        var runner = new FakeInteractiveProcessRunner();
        var launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            runner,
            new FakeProcessOutputRunner());
        launcher.SetRuntime(RuntimeKind.PowerShell);
        var command = new PluginCommand { Name = "launch", Executable = "opencode" };

        var workingDirectory = Path.Combine(
            Path.GetTempPath(), "clihub launch dir " + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workingDirectory);

        try
        {
            var started = launcher.LaunchProcess(command, workingDirectory);

            Assert.True(started);
            var launch = Assert.Single(runner.Launches);
            Assert.Equal(workingDirectory, launch.WorkingDirectory);
        }
        finally
        {
            try { Directory.Delete(workingDirectory, recursive: true); } catch { }
        }
    }

    [Fact]
    public void SetRuntime_ThenGetRuntime_ReturnsConfiguredRuntime()
    {
        var launcher = new ProcessLauncher(
            NullLogger<ProcessLauncher>.Instance,
            new FakeInteractiveProcessRunner(),
            new FakeProcessOutputRunner());

        launcher.SetRuntime(RuntimeKind.PowerShell);

        Assert.Equal(RuntimeKind.PowerShell, launcher.GetRuntime());
    }
}
