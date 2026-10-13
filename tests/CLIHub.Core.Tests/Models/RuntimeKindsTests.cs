namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class RuntimeKindsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("future-runtime")]
    [InlineData("wt")]
    public void Parse_UnknownOrMissingValue_UsesPowerShell(string? value)
    {
        Assert.Equal(RuntimeKind.PowerShell, RuntimeKinds.Parse(value));
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell, RuntimeKinds.PowerShellToken)]
    [InlineData(RuntimeKind.PowerShell7, RuntimeKinds.PowerShell7Token)]
    [InlineData(RuntimeKind.CommandPrompt, RuntimeKinds.CommandPromptToken)]
    [InlineData(RuntimeKind.GitBash, RuntimeKinds.GitBashToken)]
    public void ToToken_UsesCanonicalStoredToken(RuntimeKind runtime, string token)
    {
        Assert.Equal(token, RuntimeKinds.ToToken(runtime));
    }
}
