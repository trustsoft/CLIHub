namespace CLIHub.Core.Tests.Infrastructure.Processes;

using CLIHub.Core.Infrastructure.Processes;
using Xunit;

public sealed class RuntimeSelectorTests
{
    [Fact]
    public void SelectRuntime_ReturnsValidRuntime()
    {
        // Arrange
        var selector = new RuntimeSelector();

        // Act
        RuntimeInfo runtime = selector.SelectRuntime();

        // Assert
        Assert.NotNull(runtime);
        Assert.NotNull(runtime.ExecutablePath);
        Assert.NotEqual(string.Empty, runtime.ExecutablePath);
        Assert.True(runtime.Type is RuntimeType.WindowsTerminal or RuntimeType.Cmd);
    }

    [Fact]
    public void SelectRuntime_IsCached_ReturnsSameInstance()
    {
        // Arrange
        var selector = new RuntimeSelector();

        // Act
        RuntimeInfo first = selector.SelectRuntime();
        RuntimeInfo second = selector.SelectRuntime();

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void SelectRuntime_ReturnsCmdOrWindowsTerminal()
    {
        // Arrange
        var selector = new RuntimeSelector();

        // Act
        RuntimeInfo runtime = selector.SelectRuntime();

        // Assert
        if (runtime.Type == RuntimeType.WindowsTerminal)
        {
            Assert.Equal("wt.exe", runtime.ExecutablePath);
        }
        else if (runtime.Type == RuntimeType.Cmd)
        {
            Assert.Equal("cmd.exe", runtime.ExecutablePath);
        }
        else
        {
            Assert.Fail($"Unexpected runtime type: {runtime.Type}");
        }
    }
}
