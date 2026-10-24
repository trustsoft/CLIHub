namespace CLIHub.Core.Windows.Tests;

using System.Runtime.Versioning;

using CLIHub.Core.Infrastructure.Processes;

using CLIHub.Core.Models;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Xunit;

[SupportedOSPlatform("windows")]
public sealed class WindowsAgentProcessInspectorTests
{
    // Process names that exist on any interactive Windows 10/11 session, so the candidate
    // pre-filter still yields a non-empty result on every machine.
    private static readonly string[] CommonCandidateNames = ["explorer", "svchost"];

    private readonly FakeProcessCommandLineSource commandLineSource = new();

    private WindowsAgentProcessInspector CreateInspector(TimeSpan? commandLineTimeout = null) =>
        new(
            NullLogger<WindowsAgentProcessInspector>.Instance,
            this.commandLineSource,
            commandLineTimeout);

    private static IReadOnlySet<string> Candidates(params string[] names) =>
        new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

    [Fact]
    public void GetRunningProcesses_WithCommonCandidates_ReturnsNonEmptyList()
    {
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.NotNull(processes);
        Assert.NotEmpty(processes); // Should find at least some candidate processes
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_FiltersOutSystemProcesses()
    {
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.All(processes, p => Assert.True(p.ProcessId >= 100, "System processes (PID < 100) should be filtered out"));
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_ReturnsValidExecutablePaths()
    {
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);

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
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);

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
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(), AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.NotNull(processes);
        Assert.Empty(processes);
    }

    [Fact]
    public void GetRunningProcesses_WithNonMatchingCandidates_ReturnsEmpty()
    {
        var inspector = this.CreateInspector();

        // Act
        var processes = inspector.GetRunningProcesses(Candidates("definitely-not-a-real-process-name"), AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.NotNull(processes);
        Assert.Empty(processes);
    }

    [Fact]
    public void GetRunningProcesses_WithCandidates_HandlesAccessDeniedGracefully()
    {
        var inspector = this.CreateInspector();

        // Act - should not throw even if some processes are inaccessible
        var exception = Record.Exception(() => inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void GetRunningProcesses_CommandLineDetail_AttachesCommandLineByProcessId()
    {
        var inspector = this.CreateInspector();
        var snapshot = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);
        Assert.NotEmpty(snapshot);
        var taggedProcessId = snapshot[0].ProcessId;
        this.commandLineSource.CommandLines[taggedProcessId] =
            @"node.exe C:\Users\Maxim\AppData\Roaming\npm\node_modules\@opencode\cli\bin\opencode.js run";

        // Act
        var processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.CommandLine);

        // Assert — the mapped PID carries the command line; every other instance carries null.
        var tagged = processes.Single(p => p.ProcessId == taggedProcessId);
        Assert.Equal(this.commandLineSource.CommandLines[taggedProcessId], tagged.CommandLine);
        Assert.All(
            processes.Where(p => p.ProcessId != taggedProcessId),
            p => Assert.Null(p.CommandLine));
    }

    [Fact]
    public void GetRunningProcesses_ExecutableNameDetail_DoesNotQueryCommandLines()
    {
        var inspector = this.CreateInspector();

        // Act
        inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.ExecutableName);

        // Assert
        Assert.Equal(0, this.commandLineSource.CallCount);
    }

    [Fact]
    public void GetRunningProcesses_NoCandidateProcesses_DoesNotQueryCommandLines()
    {
        var inspector = this.CreateInspector();

        // Act
        inspector.GetRunningProcesses(Candidates("definitely-not-a-real-process-name"), AgentProcessMatchDetail.CommandLine);

        // Assert
        Assert.Equal(0, this.commandLineSource.CallCount);
    }

    [Fact]
    public void GetRunningProcesses_SourceThrows_LogsAndDegrades()
    {
        this.commandLineSource.Throw = new InvalidOperationException("WMI broke");
        var logger = new Mock<ILogger<WindowsAgentProcessInspector>>();
        var throwingInspector = new WindowsAgentProcessInspector(logger.Object, this.commandLineSource);

        // Act
        Exception? failure = null;
        IReadOnlyList<AgentProcessInstance> processes = [];
        try
        {
            processes = throwingInspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.CommandLine);
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        // Assert — no exception escapes; every instance degrades to a null command line; the
        // degradation warning is logged.
        Assert.Null(failure);
        Assert.NotEmpty(processes);
        Assert.All(processes, p => Assert.Null(p.CommandLine));
        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => true),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void GetRunningProcesses_SourceExceedsTimeout_Degrades()
    {
        var inspector = this.CreateInspector(TimeSpan.FromMilliseconds(200));
        using var gate = new ManualResetEventSlim(false);
        this.commandLineSource.Block = gate;

        // Act — must return within a bounded wait even though the source is blocked.
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        IReadOnlyList<AgentProcessInstance> processes;
        try
        {
            processes = inspector.GetRunningProcesses(Candidates(CommonCandidateNames), AgentProcessMatchDetail.CommandLine);
        }
        finally
        {
            gate.Set();
        }

        stopwatch.Stop();

        // Assert — the call returned without waiting on the blocked source, and every command
        // line is null (degraded).
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Call took {stopwatch.Elapsed}; the hard budget did not bound it");
        Assert.NotEmpty(processes);
        Assert.All(processes, p => Assert.Null(p.CommandLine));
    }
}
