namespace CLIHub.Tests.Formatting;

using CLIHub.Core.Formatting;

public class MiddleEllipsisFormatterTests
{
    private const string LongPath = @"C:\Users\Maxim\YandexDisk\Projects\Trustsoft.TextTables";

    [Fact]
    public void Format_TextShorterThanBudget_ReturnsTextUnchanged()
    {
        var result = MiddleEllipsisFormatter.Format(@"C:\Projects\Bondly", 40);

        Assert.Equal(@"C:\Projects\Bondly", result);
    }

    [Fact]
    public void Format_TextExactlyAtBudget_ReturnsTextUnchanged()
    {
        var result = MiddleEllipsisFormatter.Format(@"C:\Projects\Bondly", 18);

        Assert.Equal(@"C:\Projects\Bondly", result);
    }

    [Fact]
    public void Format_LongPathWithSeparators_KeepsBothEndsWithinBudget()
    {
        var result = MiddleEllipsisFormatter.Format(LongPath, 40);

        Assert.True(result.Length <= 40, $"expected at most 40 characters, got '{result}' ({result.Length})");
        Assert.StartsWith(@"C:\Users\Maxim\", result);
        Assert.EndsWith("Trustsoft.TextTables", result);
        Assert.Contains(MiddleEllipsisFormatter.Ellipsis, result);
    }

    [Fact]
    public void Format_LongPathWithSeparators_CutsHeadAtSegmentBoundary()
    {
        var result = MiddleEllipsisFormatter.Format(LongPath, 40);

        Assert.Equal(@"C:\Users\Maxim\…Trustsoft.TextTables", result);
    }

    [Fact]
    public void Format_TailStartsInsideASegment_CutsTailAtSegmentBoundary()
    {
        var result = MiddleEllipsisFormatter.Format(@"C:\VeryLongSegmentName12345\short", 24);

        Assert.Equal(@"C:\…short", result);
    }

    [Fact]
    public void Format_SingleLongSegmentWithoutSeparator_KeepsBothEnds()
    {
        const string text = "abcdefghijklmnopqrstuvwxyz0123456789";

        var result = MiddleEllipsisFormatter.Format(text, 20);

        Assert.Equal("abcdefghij…123456789", result);
    }

    [Fact]
    public void Format_BudgetTooSmallToKeepBothEnds_FitsAndKeepsTheEllipsis()
    {
        var result = MiddleEllipsisFormatter.Format(LongPath, 2);

        Assert.Equal(2, result.Length);
        Assert.Equal("C…", result);
    }

    [Fact]
    public void Format_BudgetOfOneCharacter_ReturnsOnlyTheEllipsis()
    {
        var result = MiddleEllipsisFormatter.Format(LongPath, 1);

        Assert.Equal("…", result);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(24)]
    [InlineData(40)]
    [InlineData(53)]
    public void Format_StaysWithinTheRequestedBudget(int maxLength)
    {
        var result = MiddleEllipsisFormatter.Format(LongPath, maxLength);

        Assert.True(
            result.Length <= maxLength,
            $"maxLength {maxLength} produced '{result}' ({result.Length} characters)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Format_MissingText_ReturnsEmpty(string? text)
    {
        Assert.Equal(string.Empty, MiddleEllipsisFormatter.Format(text, 40));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Format_NonPositiveBudget_ReturnsEmpty(int maxLength)
    {
        Assert.Equal(string.Empty, MiddleEllipsisFormatter.Format(LongPath, maxLength));
    }
}
