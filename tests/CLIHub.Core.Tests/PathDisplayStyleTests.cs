namespace CLIHub.Tests.Formatting;

using CLIHub.Core.Models;

public class PathDisplayStyleTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_MissingValue_ReturnsLeftTrim(string? value)
    {
        Assert.Equal(PathDisplayStyle.LeftTrim, PathDisplayStyles.Parse(value));
    }

    [Theory]
    [InlineData("leftTrim")]
    [InlineData("left-trim")]
    [InlineData("LEFTTRIM")]
    [InlineData("left")]
    public void Parse_LeftTrimTokens_ReturnLeftTrim(string value)
    {
        Assert.Equal(PathDisplayStyle.LeftTrim, PathDisplayStyles.Parse(value));
    }

    [Theory]
    [InlineData("middleEllipsis")]
    [InlineData("middle-ellipsis")]
    [InlineData("MiddleEllipsis")]
    [InlineData("middle")]
    public void Parse_MiddleEllipsisTokens_ReturnMiddleEllipsis(string value)
    {
        Assert.Equal(PathDisplayStyle.MiddleEllipsis, PathDisplayStyles.Parse(value));
    }

    [Theory]
    [InlineData("nonsense")]
    [InlineData("33")]
    [InlineData("ellipsis-left")]
    public void Parse_UnrecognizedValue_FallsBackToTheDefault(string value)
    {
        Assert.Equal(PathDisplayStyles.Default, PathDisplayStyles.Parse(value));
    }

    [Fact]
    public void Default_IsLeftTrim()
    {
        Assert.Equal(PathDisplayStyle.LeftTrim, PathDisplayStyles.Default);
    }

    [Theory]
    [InlineData(PathDisplayStyle.LeftTrim)]
    [InlineData(PathDisplayStyle.MiddleEllipsis)]
    public void ToToken_RoundTripsThroughParse(PathDisplayStyle style)
    {
        Assert.Equal(style, PathDisplayStyles.Parse(PathDisplayStyles.ToToken(style)));
    }
}
