namespace CLIHub.Tests.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
/// In-memory IProcessLauncher recording calls, for testing command routing.
/// </summary>
public class FakeProcessLauncher : IProcessLauncher
{
    public List<(PluginCommand Command, string WorkingDirectory)> Launches { get; } = new();
    public List<(string Executable, string? Arguments, string WorkingDirectory)> Captures { get; } = new();

    public bool LaunchResult { get; set; } = true;
    public ProcessCaptureResult CaptureResult { get; set; } = new(true, 0, "1.2.3", string.Empty);

    public bool LaunchProcess(PluginCommand command, string workingDirectory)
    {
        Launches.Add((command, workingDirectory));
        return LaunchResult;
    }

    public Task<ProcessCaptureResult> CaptureOutputAsync(
        string executable, string? arguments, string workingDirectory, CancellationToken cancellationToken = default)
    {
        Captures.Add((executable, arguments, workingDirectory));
        return Task.FromResult(CaptureResult);
    }

    public void SetTerminalExecutable(string terminalExecutable) { }

    public string GetTerminalExecutable() => "wt.exe";
}
