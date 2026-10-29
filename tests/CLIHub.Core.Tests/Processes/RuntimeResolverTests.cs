namespace CLIHub.Core.Tests.Processes;

using Moq;

using CLIHub.Core.Infrastructure.Processes;
using CLIHub.Core.Models;
using CLIHub.Core.Processes;

/// <summary>
///   Unit tests for <see cref="RuntimeResolver" />.
/// </summary>
public class RuntimeResolverTests
{
    [Fact]
    public async Task ResolveAsync_PowerShellDefault_WhenLocatorReturnsNull()
    {
        // Arrange
        var mockLocator = new Mock<IRuntimeExecutableLocator>();
        mockLocator.Setup(x => x.Locate(RuntimeKind.PowerShell)).Returns((string?)null);
        var resolver = new RuntimeResolver(mockLocator.Object);

        // Act
        var result = await resolver.ResolveAsync(RuntimeKind.PowerShell);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("powershell.exe", result.ExecutablePath);
        Assert.Equal(RuntimeKind.PowerShell, result.Kind);
    }

    [Fact]
    public async Task ResolveAsync_CommandPromptDefault_WhenLocatorReturnsNull()
    {
        // Arrange
        var mockLocator = new Mock<IRuntimeExecutableLocator>();
        mockLocator.Setup(x => x.Locate(RuntimeKind.CommandPrompt)).Returns((string?)null);
        var resolver = new RuntimeResolver(mockLocator.Object);

        // Act
        var result = await resolver.ResolveAsync(RuntimeKind.CommandPrompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cmd.exe", result.ExecutablePath);
        Assert.Equal(RuntimeKind.CommandPrompt, result.Kind);
    }

    [Fact]
    public async Task ResolveAsync_GitBashNull_WhenNotLocated()
    {
        // Arrange
        var mockLocator = new Mock<IRuntimeExecutableLocator>();
        mockLocator.Setup(x => x.Locate(RuntimeKind.GitBash)).Returns((string?)null);
        var resolver = new RuntimeResolver(mockLocator.Object);

        // Act
        var result = await resolver.ResolveAsync(RuntimeKind.GitBash);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_PowerShell7Null_WhenNotLocated()
    {
        // Arrange
        var mockLocator = new Mock<IRuntimeExecutableLocator>();
        mockLocator.Setup(x => x.Locate(RuntimeKind.PowerShell7)).Returns((string?)null);
        var resolver = new RuntimeResolver(mockLocator.Object);

        // Act
        var result = await resolver.ResolveAsync(RuntimeKind.PowerShell7);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsRuntimeExecutable_WhenLocated()
    {
        // Arrange
        var mockLocator = new Mock<IRuntimeExecutableLocator>();
        mockLocator.Setup(x => x.Locate(RuntimeKind.GitBash)).Returns(@"C:\Program Files\Git\bin\bash.exe");
        var resolver = new RuntimeResolver(mockLocator.Object);

        // Act
        var result = await resolver.ResolveAsync(RuntimeKind.GitBash);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(@"C:\Program Files\Git\bin\bash.exe", result.ExecutablePath);
        Assert.Equal(RuntimeKind.GitBash, result.Kind);
    }
}
