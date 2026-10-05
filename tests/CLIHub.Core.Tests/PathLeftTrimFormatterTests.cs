namespace CLIHub.Tests.Formatting;

using CLIHub.Core.Formatting;

public class PathLeftTrimFormatterTests
{
    private const string LongPath = @"C:\Users\Maxim\YandexDisk\Projects\Trustsoft.TextTables";

    [Fact]
    public void Format_PathShorterThanBudget_ReturnsPathUnchanged()
    {
        var result = PathLeftTrimFormatter.Format(@"C:\Projects\Bondly", 40);

        Assert.Equal(@"C:\Projects\Bondly", result);
    }

    [Fact]
    public void Format_LongPath_KeepsTheEndAndDropsTheBeginning()
    {
        var result = PathLeftTrimFormatter.Format(LongPath, 21);

        Assert.Equal("…Trustsoft.TextTables", result);
    }

    [Fact]
    public void Format_TailStartsInsideASegment_ResumesAtASegmentBoundary()
    {
        var result = PathLeftTrimFormatter.Format(LongPath, 22);

        Assert.Equal("…Trustsoft.TextTables", result);
    }

    [Fact]
    public void Format_TailWithoutSeparator_KeepsTheRawTail()
    {
        var result = PathLeftTrimFormatter.Format(LongPath, 20);

        Assert.Equal("…rustsoft.TextTables", result);
    }

    [Fact]
    public void Format_TrailingSeparator_IsKept()
    {
        var result = PathLeftTrimFormatter.Format("C:\\aaaa\\bbbb\\", 6);

        Assert.Equal("…bbbb\\", result);
    }

    [Fact]
    public void Format_BudgetOfTwo_KeepsASingleCharacterAndTheEllipsis()
    {
        var result = PathLeftTrimFormatter.Format(LongPath, 2);

        Assert.Equal("…s", result);
    }

    [Fact]
    public void Format_BudgetOfOneCharacter_ReturnsOnlyTheEllipsis()
    {
        var result = PathLeftTrimFormatter.Format(LongPath, 1);

        Assert.Equal("…", result);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(20)]
    [InlineData(21)]
    [InlineData(22)]
    [InlineData(40)]
    [InlineData(54)]
    public void Format_StaysWithinTheRequestedBudget(int maxLength)
    {
        var result = PathLeftTrimFormatter.Format(LongPath, maxLength);

        Assert.True(
            result.Length <= maxLength,
            $"maxLength {maxLength} produced '{result}' ({result.Length} characters)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Format_MissingText_ReturnsEmpty(string? text)
    {
        Assert.Equal(string.Empty, PathLeftTrimFormatter.Format(text, 40));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Format_NonPositiveBudget_ReturnsEmpty(int maxLength)
    {
        Assert.Equal(string.Empty, PathLeftTrimFormatter.Format(LongPath, maxLength));
    }
}
