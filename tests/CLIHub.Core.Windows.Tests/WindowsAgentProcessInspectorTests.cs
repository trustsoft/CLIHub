namespace CLIHub.Core.Windows.Tests;

using System.Runtime.Versioning;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

[SupportedOSPlatform("windows")]
public sealed class WindowsAgentProcessInspectorTests
{
    // Process names that exist on any interactive Windows 10/11 session, so the candidate
    // pre-filter still yields a non-empty result on every machine.
    private static readonly string[] CommonCandidateNames = ["explorer", "svchost"];

    private readonly WindowsAgentProcessInspector inspector;

    public WindowsAgentProcessInspectorTests()
    {
        this.inspector = new WindowsAgentProcessInspector(NullLogger<WindowsAgentProcessInspector>.Instance);
    }

    private static IReadOnlySet<string> Candidates(params string[] names) =>
        new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void GetRunningProcesses_WithCommonCandidates_ReturnsNonEmptyList()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates(CommonCandidateNames));

        // Assert
        Assert.NotNull(processes);
        Assert.NotEmpty(processes); // Should find at least some candidate processes
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_FiltersOutSystemProcesses()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates(CommonCandidateNames));

        // Assert
        Assert.All(processes, p => Assert.True(p.ProcessId >= 100, "System processes (PID < 100) should be filtered out"));
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_ReturnsValidExecutablePaths()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates(CommonCandidateNames));

        // Assert
        Assert.All(processes, p =>
        {
            Assert.False(string.IsNullOrEmpty(p.ExecutablePath), "Executable path should not be null or empty");
            Assert.True(Path.IsPathRooted(p.ExecutablePath), "Executable path should be absolute");
        });
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_ReturnsValidStartTimes()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates(CommonCandidateNames));

        // Assert
        Assert.All(processes, p =>
        {
            Assert.True(p.StartTime > DateTime.MinValue, "Start time should be valid");
            Assert.True(p.StartTime <= DateTime.Now, "Start time should be in the past");
        });
    }

    [Fact]
    public void GetRunningProcesses_WithEmptyCandidates_ReturnsEmpty()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates());

        // Assert
        Assert.NotNull(processes);
        Assert.Empty(processes);
    }

    [Fact]
    public void GetRunningProcesses_WithNonMatchingCandidates_ReturnsEmpty()
    {
        // Act
        var processes = this.inspector.GetRunningProcesses(Candidates("definitely-not-a-real-process-name"));

        // Assert
        Assert.NotNull(processes);
        Assert.Empty(processes);
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_HandlesAccessDeniedGracefully()
    {
        // Act - should not throw even if some processes are inaccessible
        var exception = Record.Exception(() => this.inspector.GetRunningProcesses(Candidates(CommonCandidateNames)));

        // Assert
        Assert.Null(exception);
    }
}
