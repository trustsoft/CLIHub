namespace CLIHub.Core.Tests.Processes;

using CLIHub.Core.Models;
using CLIHub.Core.Processes;

/// <summary>
///   Unit tests for <see cref="CommandLineBuilderAdapter" />.
/// </summary>
public class CommandLineBuilderAdapterTests
{
    private readonly CommandLineBuilderAdapter adapter = new();

    [Fact]
    public void BuildInteractive_PowerShellCommand_ReturnsPowerShellInvocation()
    {
        // Arrange
        var runtime = RuntimeExecutable.Create(RuntimeKind.PowerShell, "powershell.exe");
        var command = new PluginCommand { Name = "test", Executable = "agent.exe", Arguments = "--run" };

        // Act
        var (fileName, arguments) = this.adapter.BuildInteractive(runtime, command, @"C:\work");

        // Assert
        Assert.Equal("powershell.exe", fileName);
        Assert.StartsWith("-NoExit -EncodedCommand", arguments);
    }

    [Fact]
    public void BuildInteractive_PowerShell7Command_UsesPowerShell7Path()
    {
        // Arrange
        var runtime = RuntimeExecutable.Create(RuntimeKind.PowerShell7, @"C:\Program Files\PowerShell\7\pwsh.exe");
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };

        // Act
        var (fileName, arguments) = this.adapter.BuildInteractive(runtime, command, @"C:\work");

        // Assert
        Assert.Equal(@"C:\Program Files\PowerShell\7\pwsh.exe", fileName);
        Assert.StartsWith("-NoExit -EncodedCommand", arguments);
    }

    [Fact]
    public void BuildInteractive_GitBashCommand_UsesGitBashPath()
    {
        // Arrange
        var runtime = RuntimeExecutable.Create(RuntimeKind.GitBash, @"C:\Program Files\Git\bin\bash.exe");
        var command = new PluginCommand { Name = "test", Executable = "agent.exe" };

        // Act
        var (fileName, arguments) = this.adapter.BuildInteractive(runtime, command, @"C:\work");

        // Assert
        Assert.Equal(@"C:\Program Files\Git\bin\bash.exe", fileName);
        Assert.StartsWith("-lic", arguments);
    }

    [Fact]
    public void BuildInteractive_CommandPrompt_ReturnsCmdInvocation()
    {
        // Arrange
        var runtime = RuntimeExecutable.Create(RuntimeKind.CommandPrompt, "cmd.exe");
        var command = new PluginCommand { Name = "test", Executable = "agent.exe", Arguments = "-v" };

        // Act
        var (fileName, arguments) = this.adapter.BuildInteractive(runtime, command, @"C:\work");

        // Assert
        Assert.Equal("cmd.exe", fileName);
        Assert.StartsWith("/d /k", arguments);
    }

    [Fact]
    public void BuildCaptured_SimpleCommand_ReturnsCmdWrapper()
    {
        // Act
        var (fileName, arguments) = this.adapter.BuildCaptured("echo", "hello");

        // Assert
        Assert.EndsWith("cmd.exe", fileName);
        Assert.Contains("/c", arguments);
        Assert.Contains("echo", arguments);
        Assert.Contains("hello", arguments);
    }

    [Fact]
    public void BuildCaptured_NoArguments_OmitsArguments()
    {
        // Act
        var (fileName, arguments) = this.adapter.BuildCaptured("agent.exe", null);

        // Assert
        Assert.EndsWith("cmd.exe", fileName);
        Assert.Contains("agent.exe", arguments);
    }
}
