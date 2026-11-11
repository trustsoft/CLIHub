namespace CLIHub.Core.Tests.UI;

using CLIHub.Core.UI;

public class BusyGlyphSequenceTests
{
    [Fact]
    public void GetFrame_ReturnsCorrectFrame()
    {
        string[] expectedFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

        for (var index = 0; index < expectedFrames.Length; index++)
        {
            Assert.Equal(expectedFrames[index], BusyGlyphSequence.GetFrame(index));
        }
    }

    [Fact]
    public void GetFrame_WrapsAroundAtEndOfSequence()
    {
        Assert.Equal("⠋", BusyGlyphSequence.GetFrame(10));
        Assert.Equal("⠙", BusyGlyphSequence.GetFrame(11));
        Assert.Equal("⠏", BusyGlyphSequence.GetFrame(29));
    }

    [Fact]
    public void ClampSpeed_BelowMinimum_ReturnsMinimum()
    {
        Assert.Equal(40, BusyGlyphSequence.ClampSpeed(10));
    }

    [Fact]
    public void ClampSpeed_AboveMaximum_ReturnsMaximum()
    {
        Assert.Equal(200, BusyGlyphSequence.ClampSpeed(500));
    }

    [Fact]
    public void ClampSpeed_WithinRange_ReturnsValue()
    {
        Assert.Equal(80, BusyGlyphSequence.ClampSpeed(80));
    }

    [Fact]
    public void GetReducedMotionGlyph_ReturnsFullCellGlyph()
    {
        Assert.Equal("⣿", BusyGlyphSequence.GetReducedMotionGlyph());
    }
}
