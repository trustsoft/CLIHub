namespace CLIHub.Core.Tests.Plugins;

using CLIHub.Core.Plugins;

public class AppCompatibilityRangeTests
{
    [Theory]
    [InlineData("1.1.0", true)]
    [InlineData("1.2.0", true)]
    [InlineData("1.0.9", false)]
    [InlineData("0.9.0", false)]
    public void IsSatisfied_SingleFloorConstraint_SatisfiedAtOrAboveAndUnsatisfiedBelow(
        string currentVersion, bool expected)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied(currentVersion, ">=1.1.0");

        Assert.Equal(expected, satisfied);
    }

    [Theory]
    [InlineData("1.1.0", true)]
    [InlineData("1.5.0", true)]
    [InlineData("1.9.9", true)]
    [InlineData("2.0.0", false)]
    [InlineData("2.1.0", false)]
    [InlineData("1.0.0", false)]
    public void IsSatisfied_RangeWithUpperBound_SatisfiedInsideAndUnsatisfiedAtUpperBound(
        string currentVersion, bool expected)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied(currentVersion, ">=1.1.0 <2.0.0");

        Assert.Equal(expected, satisfied);
    }

    [Theory]
    [InlineData("1.2.0", true)]
    [InlineData("1.1.9", true)]
    [InlineData("1.2.1", false)]
    public void IsSatisfied_LessThanOrEqualOperator_SatisfiedAtOrBelowBound(
        string currentVersion, bool expected)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied(currentVersion, "<=1.2.0");

        Assert.Equal(expected, satisfied);
    }

    [Theory]
    [InlineData("1.2.1", true)]
    [InlineData("1.2.0", false)]
    [InlineData("1.1.9", false)]
    public void IsSatisfied_StrictlyGreaterOperator_ExcludesBoundary(
        string currentVersion, bool expected)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied(currentVersion, ">1.2.0");

        Assert.Equal(expected, satisfied);
    }

    [Theory]
    [InlineData("1.1.0", true)]
    [InlineData("1.0.9", false)]
    [InlineData("1.1.1", false)]
    [InlineData("2.0.0", false)]
    public void IsSatisfied_EqualityOperator_MatchesExactVersionOnly(
        string currentVersion, bool expected)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied(currentVersion, "=1.1.0");

        Assert.Equal(expected, satisfied);
    }

    [Fact]
    public void IsSatisfied_CommaSeparatedConstraints_EvaluatesSameAsSpaceSeparated()
    {
        var commaSeparated = AppCompatibilityRange.IsSatisfied("1.5.0", ">=1.1.0,<2.0.0");
        var spaceSeparated = AppCompatibilityRange.IsSatisfied("1.5.0", ">=1.1.0 <2.0.0");

        Assert.True(commaSeparated);
        Assert.Equal(spaceSeparated, commaSeparated);
    }

    [Fact]
    public void IsSatisfied_MixedCommaAndSpaceSeparators_EvaluatesSameResult()
    {
        var mixed = AppCompatibilityRange.IsSatisfied("1.5.0", ">=1.1.0, <2.0.0");
        var spaceOnly = AppCompatibilityRange.IsSatisfied("1.5.0", ">=1.1.0 <2.0.0");

        Assert.True(mixed);
        Assert.Equal(spaceOnly, mixed);
    }

    [Fact]
    public void IsSatisfied_TwoSegmentVersion_TreatedAsMissingPatchZero()
    {
        var atBoundary = AppCompatibilityRange.IsSatisfied("1.1.0", ">=1.1");
        var aboveBoundary = AppCompatibilityRange.IsSatisfied("1.1.5", ">=1.1");
        var belowBoundary = AppCompatibilityRange.IsSatisfied("1.0.9", ">=1.1");

        Assert.True(atBoundary);
        Assert.True(aboveBoundary);
        Assert.False(belowBoundary);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("1.0.0", "1.1.0")]
    [InlineData("1.1.0", "1.1.0")]
    [InlineData("1.2.3", "1.1.0")]
    [InlineData("2.0.0", "1.1.0")]
    [InlineData("1.5.0", "2.0.0")]
    [InlineData("2.1.0", "2.0.0")]
    [InlineData("1.1", "1.1")]
    [InlineData("1", "1.0.0")]
    [InlineData("1.10.0", "1.9.5")]
    public void IsSatisfied_V1FloorEquivalence_AgreesWithAppVersionComparator(
        string currentVersion, string minAppVersion)
    {
        var expected = AppVersionComparator.IsSatisfied(currentVersion, minAppVersion);

        var actual = AppCompatibilityRange.IsSatisfied(currentVersion, ">=" + minAppVersion);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryParse_InvalidOperator_ReturnsFalse()
    {
        var parsed = AppCompatibilityRange.TryParse("~1.0.0", out var constraints);

        Assert.False(parsed);
        Assert.Empty(constraints);
    }

    [Fact]
    public void TryParse_NonNumericSegment_ReturnsFalse()
    {
        var parsed = AppCompatibilityRange.TryParse(">=1.x", out var constraints);

        Assert.False(parsed);
        Assert.Empty(constraints);
    }

    [Fact]
    public void TryParse_TooManySegments_ReturnsFalse()
    {
        var parsed = AppCompatibilityRange.TryParse(">=1.2.3.4", out var constraints);

        Assert.False(parsed);
        Assert.Empty(constraints);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TryParse_EmptyOrWhitespaceOnlyInput_ReturnsFalse(string compatibility)
    {
        var parsed = AppCompatibilityRange.TryParse(compatibility, out var constraints);

        Assert.False(parsed);
        Assert.Empty(constraints);
    }

    [Theory]
    [InlineData("~1.0.0")]
    [InlineData(">=1.x")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSatisfied_UnparseableRange_FailsClosed(string compatibility)
    {
        var satisfied = AppCompatibilityRange.IsSatisfied("1.5.0", compatibility);

        Assert.False(satisfied);
    }

    [Fact]
    public void TryParse_ValidRange_ReturnsConstraintsInOrder()
    {
        var parsed = AppCompatibilityRange.TryParse(">=1.1.0 <2.0.0", out var constraints);

        Assert.True(parsed);
        Assert.Equal(2, constraints.Count);
        Assert.Equal(">=", constraints[0].Op);
        Assert.Equal((1, 1, 0), constraints[0].Version);
        Assert.Equal("<", constraints[1].Op);
        Assert.Equal((2, 0, 0), constraints[1].Version);
    }
}
