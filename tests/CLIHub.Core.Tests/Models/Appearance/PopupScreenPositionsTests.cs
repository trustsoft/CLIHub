namespace CLIHub.Core.Tests.Models;

using CLIHub.Core.Models;

public class PopupScreenPositionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    public void Parse_UnknownOrMissingValue_UsesCenter(string? value)
    {
        Assert.Equal(PopupScreenPosition.Center, PopupScreenPositions.Parse(value));
    }

    [Theory]
    [InlineData("center", PopupScreenPosition.Center)]
    [InlineData("topCenter", PopupScreenPosition.TopCenter)]
    [InlineData("cursorRelative", PopupScreenPosition.CursorRelative)]
    [InlineData("primary", PopupScreenPosition.PrimaryScreen)]
    public void TryParse_CanonicalTokens_ParseCorrectly(string value, PopupScreenPosition expected)
    {
        Assert.True(PopupScreenPositions.TryParse(value, out var position));
        Assert.Equal(expected, position);
    }

    [Theory]
    [InlineData("centre")]
    [InlineData("center-of-active-monitor")]
    [InlineData("top-center")]
    [InlineData("top")]
    [InlineData("cursor-relative")]
    [InlineData("cursor")]
    [InlineData("primary-screen")]
    public void Parse_OldAliases_FallBackToCenter(string value)
    {
        Assert.False(PopupScreenPositions.TryParse(value, out _));
        Assert.Equal(PopupScreenPosition.Center, PopupScreenPositions.Parse(value));
    }

    [Theory]
    [InlineData(PopupScreenPosition.Center, PopupScreenPositions.CenterToken)]
    [InlineData(PopupScreenPosition.TopCenter, PopupScreenPositions.TopCenterToken)]
    [InlineData(PopupScreenPosition.CursorRelative, PopupScreenPositions.CursorRelativeToken)]
    [InlineData(PopupScreenPosition.PrimaryScreen, PopupScreenPositions.PrimaryToken)]
    public void ToToken_UsesCanonicalStoredToken(PopupScreenPosition position, string token)
    {
        Assert.Equal(token, PopupScreenPositions.ToToken(position));
    }

    [Fact]
    public void Parse_RoundTripsEveryPosition()
    {
        foreach (var position in Enum.GetValues<PopupScreenPosition>())
        {
            Assert.Equal(position, PopupScreenPositions.Parse(PopupScreenPositions.ToToken(position)));
        }
    }
}
