using CLIHub.Core.Services;

namespace CLIHub.Core.Tests;

public sealed class CommandBuilderTests
{
    private const string Project = "C:\\projects\\demo";

    [Fact]
    public void Build_Cmd_UsesCmdWithKeepOpen()
    {
        var command = CommandBuilder.Build("cmd", "claude", Project);

        Assert.Equal("cmd.exe", command.FileName);
        Assert.Equal("/k \"claude\"", command.Arguments);
        Assert.Equal(Project, command.WorkingDirectory);
    }

    [Fact]
    public void Build_Ps_UsesPowershellNoExit()
    {
        var command = CommandBuilder.Build("ps", "claude", Project);

        Assert.Equal("powershell.exe", command.FileName);
        Assert.Equal("-NoExit -Command \"claude\"", command.Arguments);
        Assert.Equal(Project, command.WorkingDirectory);
    }

    [Fact]
    public void Build_Wt_WrapsCommandInCmd()
    {
        var command = CommandBuilder.Build("wt", "claude", Project);

        Assert.Equal("wt.exe", command.FileName);
        Assert.Equal($"-d \"{Project}\" cmd /k \"claude\"", command.Arguments);
        Assert.Equal(Project, command.WorkingDirectory);
    }
}
