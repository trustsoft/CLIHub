using CLIHub.Core.Abstractions;
using CLIHub.Core.Models;

namespace CLIHub.Core.Tests;

internal sealed class CapturingProcessRunner : IProcessRunner
{
    public List<(string FileName, string Arguments, string WorkingDirectory)> Started { get; } = new();

    public List<(string FileName, string Arguments, int TimeoutSeconds)> Probes { get; } = new();

    public Func<string, bool> CommandExistsHandler { get; set; } = _ => true;

    public Func<string, string, int, ProbeResult> ProbeHandler { get; set; } =
        (_, _, _) => ProbeResult.Failed;

    public bool CommandExists(string command) => CommandExistsHandler(command);

    public void StartDetached(string fileName, string arguments, string workingDirectory) =>
        Started.Add((fileName, arguments, workingDirectory));

    public ProbeResult RunProbe(string fileName, string arguments, int timeoutSeconds)
    {
        Probes.Add((fileName, arguments, timeoutSeconds));
        return ProbeHandler(fileName, arguments, timeoutSeconds);
    }
}
