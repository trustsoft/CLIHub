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
