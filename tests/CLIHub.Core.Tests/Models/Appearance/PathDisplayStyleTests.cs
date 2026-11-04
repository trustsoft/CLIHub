namespace CLIHub.Core.Tests.Models;

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
    [InlineData("leftTrim", PathDisplayStyle.LeftTrim)]
    [InlineData("LEFTTRIM", PathDisplayStyle.LeftTrim)]
    [InlineData("LeftTrim", PathDisplayStyle.LeftTrim)]
    [InlineData("middleEllipsis", PathDisplayStyle.MiddleEllipsis)]
    [InlineData("MiddleEllipsis", PathDisplayStyle.MiddleEllipsis)]
    [InlineData("MIDDLEELLIPSIS", PathDisplayStyle.MiddleEllipsis)]
    public void Parse_CanonicalTokens_ParseCorrectly(string value, PathDisplayStyle expected)
    {
        Assert.Equal(expected, PathDisplayStyles.Parse(value));
    }

    [Theory]
    [InlineData("left-trim")]
    [InlineData("left")]
    [InlineData("middle-ellipsis")]
    [InlineData("middle")]
    public void Parse_OldAliases_FallBackToLeftTrim(string value)
    {
        Assert.Equal(PathDisplayStyle.LeftTrim, PathDisplayStyles.Parse(value));
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
