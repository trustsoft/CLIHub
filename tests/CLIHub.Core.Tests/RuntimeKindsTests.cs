namespace CLIHub.Tests.Models;

using CLIHub.Core.Models;

public class RuntimeKindsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("future-runtime")]
    public void Parse_UnknownOrMissingValue_UsesWindowsTerminal(string? value)
    {
        Assert.Equal(RuntimeKind.WindowsTerminal, RuntimeKinds.Parse(value));
    }

    [Theory]
    [InlineData(RuntimeKind.WindowsTerminal, RuntimeKinds.WindowsTerminalToken)]
    [InlineData(RuntimeKind.CommandPrompt, RuntimeKinds.CommandPromptToken)]
    [InlineData(RuntimeKind.PowerShell, RuntimeKinds.PowerShellToken)]
    public void ToToken_UsesCanonicalStoredToken(RuntimeKind runtime, string token)
    {
        Assert.Equal(token, RuntimeKinds.ToToken(runtime));
    }
}
