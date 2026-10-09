namespace CLIHub.Core.Tests.Infrastructure.Processes;

using System.Runtime.Versioning;

using CLIHub.Core.Infrastructure.Processes;

using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

[SupportedOSPlatform("windows")]
public sealed class WindowsAgentProcessInspectorTests
{
    private readonly WindowsAgentProcessInspector _inspector;

    public WindowsAgentProcessInspectorTests()
    {
        _inspector = new WindowsAgentProcessInspector(NullLogger<WindowsAgentProcessInspector>.Instance);
    }

    [Fact]
    public void GetRunningProcesses_ReturnsNonEmptyList()
    {
        // Act
        var processes = _inspector.GetRunningProcesses();

        // Assert
        Assert.NotNull(processes);
        Assert.NotEmpty(processes); // Should find at least some processes
    }

    [Fact]
    public void GetRunningProcesses_FiltersOutSystemProcesses()
    {
        // Act
        var processes = _inspector.GetRunningProcesses();

        // Assert
        Assert.All(processes, p => Assert.True(p.ProcessId >= 100, "System processes (PID < 100) should be filtered out"));
    }

    [Fact]
    public void GetRunningProcesses_ReturnsValidExecutablePaths()
    {
        // Act
        var processes = _inspector.GetRunningProcesses();

        // Assert
        Assert.All(processes, p =>
        {
            Assert.False(string.IsNullOrEmpty(p.ExecutablePath), "Executable path should not be null or empty");
            Assert.True(Path.IsPathRooted(p.ExecutablePath), "Executable path should be absolute");
        });
    }

    [Fact]
    public void GetRunningProcesses_ReturnsValidStartTimes()
    {
        // Act
        var processes = _inspector.GetRunningProcesses();

        // Assert
        Assert.All(processes, p =>
        {
            Assert.True(p.StartTime > DateTime.MinValue, "Start time should be valid");
            Assert.True(p.StartTime <= DateTime.Now, "Start time should be in the past");
        });
    }

    [Fact]
    public void GetRunningProcesses_HandlesAccessDeniedGracefully()
    {
        // Act - should not throw even if some processes are inaccessible
        var exception = Record.Exception(() => _inspector.GetRunningProcesses());

        // Assert
        Assert.Null(exception);
    }
}
