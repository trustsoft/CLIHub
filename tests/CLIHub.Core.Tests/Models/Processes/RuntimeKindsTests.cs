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

    [Theory]
    [InlineData("ps", RuntimeKind.PowerShell)]
    [InlineData("PS", RuntimeKind.PowerShell)]
    [InlineData("  ps  ", RuntimeKind.PowerShell)]
    [InlineData("ps7", RuntimeKind.PowerShell7)]
    [InlineData("PS7", RuntimeKind.PowerShell7)]
    [InlineData("\tps7\n", RuntimeKind.PowerShell7)]
    [InlineData("cmd", RuntimeKind.CommandPrompt)]
    [InlineData("CMD", RuntimeKind.CommandPrompt)]
    [InlineData("  cmd  ", RuntimeKind.CommandPrompt)]
    [InlineData("bash", RuntimeKind.GitBash)]
    [InlineData("Bash", RuntimeKind.GitBash)]
    [InlineData("  bash  ", RuntimeKind.GitBash)]
    public void TryParse_CanonicalTokenAnyCaseWithSurroundingWhitespace_ResolvesKind(
        string value,
        RuntimeKind expected)
    {
        Assert.True(RuntimeKinds.TryParse(value, out var kind));
        Assert.Equal(expected, kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("future-runtime")]
    [InlineData("powershell.exe")]
    [InlineData("pwsh.exe")]
    [InlineData("command prompt")]
    [InlineData("git bash")]
    public void TryParse_UnknownMissingOrRemovedValue_ReturnsFalseWithPowerShell(string? value)
    {
        Assert.False(RuntimeKinds.TryParse(value, out var kind));
        Assert.Equal(RuntimeKind.PowerShell, kind);
    }

    [Theory]
    [InlineData(RuntimeKind.PowerShell)]
    [InlineData(RuntimeKind.PowerShell7)]
    [InlineData(RuntimeKind.CommandPrompt)]
    [InlineData(RuntimeKind.GitBash)]
    public void TryParse_StoredToken_RoundTripsToSameKind(RuntimeKind kind)
    {
        Assert.True(RuntimeKinds.TryParse(RuntimeKinds.ToToken(kind), out var parsed));
        Assert.Equal(kind, parsed);
    }
}
