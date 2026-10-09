namespace CLIHub.Tests.Fakes;

using CLIHub.Core.Infrastructure.Processes;

/// <summary>
///   Fake interactive process runner for testing.
/// </summary>
public class FakeInteractiveProcessRunner : IInteractiveProcessRunner
{
    public List<(string CommandLine, RuntimeInfo Runtime)> Launches { get; } = new();
    public int LaunchResult { get; set; } = 0;

    public Task<int> LaunchAsync(
        string commandLine,
        RuntimeInfo runtime,
        CancellationToken cancellationToken = default)
    {
        Launches.Add((commandLine, runtime));
        return Task.FromResult(LaunchResult);
    }
}
