namespace CLIHub.Tests.Services;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;

/// <summary>
///   In-memory split process runners recording calls, for testing command routing.
/// </summary>
public class FakeProcessLauncher : IInteractiveProcessRunner, IProcessOutputRunner
{
    public List<(PluginCommand Command, string WorkingDirectory)> Launches { get; } = new();
    public List<(string Executable, string? Arguments, string WorkingDirectory)> Captures { get; } = new();

    public bool LaunchResult { get; set; } = true;
    public ProcessCaptureResult CaptureResult { get; set; } = new(true, 0, "1.2.3", string.Empty);

    public TaskCompletionSource<bool>? CaptureGate { get; set; }

    public Func<string, string?, string, CancellationToken, TimeSpan?, Task<ProcessCaptureResult>>? CaptureHandler { get; set; }

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
        if (CaptureHandler is not null)
        {
            return CaptureHandler(executable, arguments, workingDirectory, cancellationToken, timeout);
        }

        if (CaptureGate != null)
        {
            return WaitForCaptureAsync(cancellationToken);
        }

        return Task.FromResult(CaptureResult);
    }

    private async Task<ProcessCaptureResult> WaitForCaptureAsync(CancellationToken cancellationToken)
    {
        await CaptureGate!.Task.WaitAsync(cancellationToken);
        return CaptureResult;
    }

    public void SetRuntime(RuntimeKind runtime) => Runtime = runtime;

    public RuntimeKind GetRuntime() => Runtime;

    public RuntimeKind Runtime { get; private set; } = RuntimeKind.WindowsTerminal;
}
