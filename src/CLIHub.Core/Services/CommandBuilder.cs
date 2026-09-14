using CLIHub.Core.Models;

namespace CLIHub.Core.Services;

public static class CommandBuilder
{
    public static ProcessCommand Build(string runtime, string command, string workingDirectory)
    {
        return runtime switch
        {
            "ps" => new ProcessCommand("powershell.exe", $"-NoExit -Command \"{command}\"", workingDirectory),
            "wt" => new ProcessCommand("wt.exe", $"-d \"{workingDirectory}\" cmd /k \"{command}\"", workingDirectory),
            _ => new ProcessCommand("cmd.exe", $"/k \"{command}\"", workingDirectory)
        };
    }
}
