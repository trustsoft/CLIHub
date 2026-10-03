namespace CLIHub.Tests.Services;

using CLIHub.Core.Interfaces;
using CLIHub.Core.Models;

/// <summary>
///   In-memory IProcessLauncher recording calls, for testing command routing.
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

    public TimeSpan? LastCaptureTimeout { get; private set; }

    public Task<ProcessCaptureResult> CaptureOutputAsync(
        string executable, string? arguments, string workingDirectory,
        CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        Captures.Add((executable, arguments, workingDirectory));
        LastCaptureTimeout = timeout;
        return Task.FromResult(CaptureResult);
    }

    public void SetRuntime(RuntimeKind runtime) => Runtime = runtime;

    public RuntimeKind GetRuntime() => Runtime;

    public RuntimeKind Runtime { get; private set; } = RuntimeKind.WindowsTerminal;
}
