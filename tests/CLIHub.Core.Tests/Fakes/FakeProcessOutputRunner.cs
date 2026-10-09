namespace CLIHub.Tests.Fakes;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Fake output runner for testing.
/// </summary>
public class FakeProcessOutputRunner : IProcessOutputRunner
{
    public List<(string CommandLine, RuntimeInfo Runtime, TimeSpan Timeout)> Runs { get; } = new();
    public ProcessResult Result { get; set; } = new()
    {
        StandardOutput = "1.2.3",
        StandardError = string.Empty,
        ExitCode = 0,
        TimedOut = false
    };

    public Task<ProcessResult> RunAsync(
        string commandLine,
        RuntimeInfo runtime,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        Runs.Add((commandLine, runtime, timeout));
        return Task.FromResult(Result);
    }
}
