namespace CLIHub.Core.Tests.Fakes;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Fake interactive process runner for testing.
/// </summary>
public class FakeInteractiveProcessRunner : IInteractiveProcessRunner
{
    public List<(string CommandLine, RuntimeInfo Runtime, string WorkingDirectory)> Launches { get; } = new();
    public int LaunchResult { get; set; } = 0;

    public Task<int> LaunchAsync(
        string commandLine,
        RuntimeInfo runtime,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        this.Launches.Add((commandLine, runtime, workingDirectory));
        return Task.FromResult(this.LaunchResult);
    }
}
