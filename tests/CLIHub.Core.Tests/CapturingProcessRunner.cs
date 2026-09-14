using CLIHub.Core.Abstractions;

namespace CLIHub.Core.Tests;

internal sealed class CapturingProcessRunner : IProcessRunner
{
    public List<(string FileName, string Arguments, string WorkingDirectory)> Started { get; } = new();

    public Func<string, bool> CommandExistsHandler { get; set; } = _ => true;

    public bool CommandExists(string command) => CommandExistsHandler(command);

    public void StartDetached(string fileName, string arguments, string workingDirectory) =>
        Started.Add((fileName, arguments, workingDirectory));
}
