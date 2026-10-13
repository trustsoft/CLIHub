namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class AppThemesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void Parse_UnknownOrMissingValue_UsesDark(string? value)
    {
        Assert.Equal(AppTheme.Dark, AppThemes.Parse(value));
    }

    [Theory]
    [InlineData("Dark", AppTheme.Dark)]
    [InlineData("light", AppTheme.Light)]
    [InlineData("System", AppTheme.System)]
    public void Parse_KnownTokens(string value, AppTheme expected)
    {
        Assert.Equal(expected, AppThemes.Parse(value));
    }

    [Fact]
    public void Parse_RoundTripsEveryTheme()
    {
        foreach (var theme in Enum.GetValues<AppTheme>())
        {
            Assert.Equal(theme, AppThemes.Parse(AppThemes.ToToken(theme)));
        }
    }
}
